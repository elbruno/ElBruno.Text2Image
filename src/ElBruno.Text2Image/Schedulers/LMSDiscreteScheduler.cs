using Microsoft.ML.OnnxRuntime.Tensors;
using ElBruno.Text2Image.Pipeline;

namespace ElBruno.Text2Image.Schedulers;

/// <summary>
/// LMS (Linear Multi-Step) Discrete Scheduler for Stable Diffusion.
/// Uses numerical integration for multi-step coefficient computation.
/// </summary>
internal sealed class LMSDiscreteScheduler : IScheduler
{
    private readonly int _numTrainTimesteps;
    private readonly string _predictionType;
    private readonly List<float> _alphasCumulativeProducts;
    private DenseTensor<float> _sigmas = null!;
    private readonly List<DenseTensor<float>> _derivatives = new();

    public float InitNoiseSigma { get; private set; }
    public List<int> Timesteps { get; private set; } = new();

    public LMSDiscreteScheduler(
        int numTrainTimesteps = 1000,
        float betaStart = 0.00085f,
        float betaEnd = 0.012f,
        string betaSchedule = "scaled_linear",
        string predictionType = "epsilon")
    {
        _numTrainTimesteps = numTrainTimesteps;
        _predictionType = predictionType;

        var betas = betaSchedule switch
        {
            "linear" => Enumerable.Range(0, numTrainTimesteps)
                .Select(i => betaStart + (betaEnd - betaStart) * i / (numTrainTimesteps - 1))
                .ToList(),
            "scaled_linear" => Linspace((float)Math.Sqrt(betaStart), (float)Math.Sqrt(betaEnd), numTrainTimesteps)
                .Select(x => x * x)
                .ToList(),
            _ => throw new ArgumentException($"beta_schedule must be 'linear' or 'scaled_linear'")
        };

        var alphas = betas.Select(b => 1f - b).ToList();
        _alphasCumulativeProducts = new List<float>(numTrainTimesteps);
        float cumprod = 1f;
        foreach (var a in alphas)
        {
            cumprod *= a;
            _alphasCumulativeProducts.Add(cumprod);
        }

        var sigmas = _alphasCumulativeProducts
            .Select(ap => (float)Math.Sqrt((1 - ap) / ap))
            .Reverse()
            .ToList();
        InitNoiseSigma = sigmas.Max();
    }

    public int[] SetTimesteps(int numInferenceSteps)
    {
        _derivatives.Clear();

        var timesteps = Linspace(0, _numTrainTimesteps - 1, numInferenceSteps);
        Timesteps = timesteps.Select(x => (int)x).Reverse().ToList();

        var sigmas = _alphasCumulativeProducts
            .Select(ap => (float)Math.Sqrt((1 - ap) / ap))
            .Reverse()
            .ToList();

        var range = Enumerable.Range(0, sigmas.Count).Select(i => (float)i).ToArray();
        var interpolatedSigmas = InterpolateSigmas(timesteps.ToArray(), range, sigmas.ToArray());

        var sigmasWithZero = interpolatedSigmas.Append(0f).ToArray();
        _sigmas = new DenseTensor<float>(sigmasWithZero, new int[] { sigmasWithZero.Length });
        InitNoiseSigma = sigmasWithZero.Max();

        return Timesteps.ToArray();
    }

    public DenseTensor<float> ScaleInput(DenseTensor<float> sample, int timestep)
    {
        int stepIndex = Timesteps.IndexOf(timestep);
        var sigma = _sigmas[stepIndex];
        sigma = (float)Math.Sqrt(sigma * sigma + 1);

        var data = sample.Buffer.ToArray();
        for (int i = 0; i < data.Length; i++)
            data[i] /= sigma;

        return new DenseTensor<float>(data, sample.Dimensions.ToArray());
    }

    public DenseTensor<float> Step(DenseTensor<float> modelOutput, int timestep, DenseTensor<float> sample, int order = 4)
    {
        int stepIndex = Timesteps.IndexOf(timestep);
        var sigma = _sigmas[stepIndex];

        // 1. Compute predicted original sample from epsilon prediction
        float[] predOrigSampleData;
        var modelData = modelOutput.Buffer.Span;
        var sampleData = sample.Buffer.Span;

        if (_predictionType == "epsilon")
        {
            predOrigSampleData = new float[modelData.Length];
            for (int i = 0; i < modelData.Length; i++)
                predOrigSampleData[i] = sampleData[i] - sigma * modelData[i];
        }
        else
        {
            throw new NotImplementedException($"prediction_type '{_predictionType}' not implemented");
        }

        // 2. Convert to ODE derivative
        var derivData = new float[sampleData.Length];
        for (int i = 0; i < derivData.Length; i++)
            derivData[i] = (sampleData[i] - predOrigSampleData[i]) / sigma;

        var derivative = new DenseTensor<float>(derivData, sample.Dimensions.ToArray());
        _derivatives.Add(derivative);

        if (_derivatives.Count > order)
            _derivatives.RemoveAt(0);

        // 3. Compute linear multistep coefficients
        order = Math.Min(stepIndex + 1, order);
        var lmsCoeffs = new double[order];
        for (int currOrder = 0; currOrder < order; currOrder++)
            lmsCoeffs[currOrder] = GetLmsCoefficient(order, stepIndex, currOrder);

        // 4. Compute previous sample from derivative path
        var revDerivatives = _derivatives.AsEnumerable().Reverse().ToList();
        var prevData = sample.Buffer.ToArray();

        for (int m = 0; m < Math.Min(lmsCoeffs.Length, revDerivatives.Count); m++)
        {
            var dData = revDerivatives[m].Buffer.Span;
            var coeff = (float)lmsCoeffs[m];
            for (int i = 0; i < prevData.Length; i++)
                prevData[i] += coeff * dData[i];
        }

        return new DenseTensor<float>(prevData, sample.Dimensions.ToArray());
    }

    /// <summary>
    /// Computes a linear multistep coefficient using Simpson's rule integration.
    /// </summary>
    private double GetLmsCoefficient(int order, int t, int currentOrder)
    {
        double LmsDerivative(double tau)
        {
            double prod = 1.0;
            for (int k = 0; k < order; k++)
            {
                if (currentOrder == k) continue;
                prod *= (tau - _sigmas[t - k]) / (_sigmas[t - currentOrder] - _sigmas[t - k]);
            }
            return prod;
        }

        // Simpson's rule integration
        double a = _sigmas[t];
        double b = _sigmas[t + 1];
        int n = 100; // number of intervals (must be even)
        double h = (b - a) / n;
        double sum = LmsDerivative(a) + LmsDerivative(b);

        for (int i = 1; i < n; i++)
        {
            double x = a + i * h;
            sum += (i % 2 == 0 ? 2 : 4) * LmsDerivative(x);
        }

        return sum * h / 3.0;
    }

    private static List<float> Linspace(float start, float end, int count)
    {
        if (count == 1) return new List<float> { start };
        var result = new List<float>(count);
        for (int i = 0; i < count; i++)
            result.Add(start + (end - start) * i / (count - 1));
        return result;
    }

    private static float[] InterpolateSigmas(float[] timesteps, float[] range, float[] sigmas)
    {
        var result = new float[timesteps.Length];
        for (int i = 0; i < timesteps.Length; i++)
        {
            var t = timesteps[i];
            int idx = Array.BinarySearch(range, t);

            if (idx >= 0)
            {
                result[i] = sigmas[idx];
            }
            else
            {
                idx = ~idx;
                if (idx == 0) result[i] = sigmas[0];
                else if (idx >= range.Length) result[i] = sigmas[^1];
                else
                {
                    float frac = (t - range[idx - 1]) / (range[idx] - range[idx - 1]);
                    result[i] = sigmas[idx - 1] + frac * (sigmas[idx] - sigmas[idx - 1]);
                }
            }
        }
        return result;
    }
}
