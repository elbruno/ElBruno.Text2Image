using Microsoft.ML.OnnxRuntime.Tensors;
using ElBruno.Text2Image.Pipeline;

namespace ElBruno.Text2Image.Schedulers;

/// <summary>
/// Euler Ancestral Discrete Scheduler for Stable Diffusion.
/// A simple and effective scheduler that doesn't require numerical integration.
/// </summary>
internal sealed class EulerAncestralDiscreteScheduler : IScheduler
{
    private readonly int _numTrainTimesteps;
    private readonly string _predictionType;
    private readonly List<float> _alphasCumulativeProducts;
    private DenseTensor<float> _sigmas = null!;

    public float InitNoiseSigma { get; private set; }
    public List<int> Timesteps { get; private set; } = new();

    public EulerAncestralDiscreteScheduler(
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
            _ => throw new ArgumentException($"beta_schedule must be 'linear' or 'scaled_linear', got '{betaSchedule}'")
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
        var timesteps = Linspace(0, _numTrainTimesteps - 1, numInferenceSteps);
        Timesteps = timesteps.Select(x => (int)x).Reverse().ToList();

        var sigmas = _alphasCumulativeProducts
            .Select(ap => (float)Math.Sqrt((1 - ap) / ap))
            .Reverse()
            .ToList();

        var range = Enumerable.Range(0, sigmas.Count).Select(i => (float)i).ToArray();
        var interpolatedSigmas = InterpolateSigmas(
            timesteps.ToArray(),
            range,
            sigmas.ToArray());

        // Append 0 at end
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

        // 1. Compute predicted original sample
        DenseTensor<float> predOriginalSample;
        if (_predictionType == "epsilon")
        {
            predOriginalSample = TensorHelper.SubtractTensors(
                sample,
                TensorHelper.MultiplyByFloat(modelOutput, sigma));
        }
        else
        {
            throw new NotImplementedException($"prediction_type '{_predictionType}' not implemented");
        }

        // 2. Compute sigma_up and sigma_down
        float sigmaFrom = _sigmas[stepIndex];
        float sigmaTo = _sigmas[stepIndex + 1];

        float sigmaUpSq = (sigmaTo * sigmaTo * (sigmaFrom * sigmaFrom - sigmaTo * sigmaTo)) / (sigmaFrom * sigmaFrom);
        float sigmaUp = sigmaUpSq < 0 ? -MathF.Sqrt(MathF.Abs(sigmaUpSq)) : MathF.Sqrt(sigmaUpSq);

        float sigmaDownSq = sigmaTo * sigmaTo - sigmaUp * sigmaUp;
        float sigmaDown = sigmaDownSq < 0 ? -MathF.Sqrt(MathF.Abs(sigmaDownSq)) : MathF.Sqrt(sigmaDownSq);

        // 3. Compute derivative
        var derivative = TensorHelper.DivideByFloat(
            TensorHelper.SubtractTensors(sample, predOriginalSample),
            sigma);

        float dt = sigmaDown - sigma;

        // 4. Compute previous sample
        var prevSample = TensorHelper.AddTensors(
            sample,
            TensorHelper.MultiplyByFloat(derivative, dt));

        // 5. Add noise
        var noise = TensorHelper.GetRandomTensor(prevSample.Dimensions.ToArray());
        prevSample = TensorHelper.AddTensors(
            prevSample,
            TensorHelper.MultiplyByFloat(noise, sigmaUp));

        return prevSample;
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
