using Microsoft.ML.OnnxRuntime.Tensors;
using ElBruno.Text2Image.Pipeline;

namespace ElBruno.Text2Image.Schedulers;

/// <summary>
/// Latent Consistency Model (LCM) Scheduler.
/// Enables very fast inference (2-4 steps) without classifier-free guidance.
/// Based on the LCM paper: https://arxiv.org/abs/2310.04378
/// </summary>
internal sealed class LCMScheduler : IScheduler
{
    private readonly int _numTrainTimesteps;
    private readonly List<float> _alphasCumulativeProducts;
    private float[] _sigmas = null!;
    private float _finalAlphaCumprod;

    public float InitNoiseSigma { get; private set; }
    public List<int> Timesteps { get; private set; } = new();

    public LCMScheduler(
        int numTrainTimesteps = 1000,
        float betaStart = 0.00085f,
        float betaEnd = 0.012f,
        string betaSchedule = "scaled_linear")
    {
        _numTrainTimesteps = numTrainTimesteps;

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

        _finalAlphaCumprod = _alphasCumulativeProducts[0];
        InitNoiseSigma = 1.0f;
    }

    public int[] SetTimesteps(int numInferenceSteps)
    {
        // LCM uses evenly spaced timesteps from the training range
        var step = _numTrainTimesteps / numInferenceSteps;
        Timesteps = Enumerable.Range(1, numInferenceSteps)
            .Select(i => Math.Min(i * step, _numTrainTimesteps) - 1)
            .Reverse()
            .ToList();

        // Compute sigmas for each timestep
        _sigmas = new float[Timesteps.Count + 1];
        for (int i = 0; i < Timesteps.Count; i++)
        {
            var alphaCumprod = _alphasCumulativeProducts[Timesteps[i]];
            _sigmas[i] = (float)Math.Sqrt((1 - alphaCumprod) / alphaCumprod);
        }
        _sigmas[Timesteps.Count] = 0f;

        return Timesteps.ToArray();
    }

    public DenseTensor<float> ScaleInput(DenseTensor<float> sample, int timestep)
    {
        // LCM doesn't need input scaling
        return sample;
    }

    public DenseTensor<float> Step(
        DenseTensor<float> modelOutput,
        int timestep,
        DenseTensor<float> sample,
        int order = 4)
    {
        int stepIndex = Timesteps.IndexOf(timestep);
        var alphaProdT = _alphasCumulativeProducts[timestep];

        // Get alpha_prod for the previous timestep
        float alphaProdTPrev;
        if (stepIndex < Timesteps.Count - 1)
        {
            alphaProdTPrev = _alphasCumulativeProducts[Timesteps[stepIndex + 1]];
        }
        else
        {
            alphaProdTPrev = _finalAlphaCumprod;
        }

        var betaProdT = 1f - alphaProdT;
        var betaProdTPrev = 1f - alphaProdTPrev;

        // Compute predicted original sample (x_0) from epsilon prediction
        var sqrtAlphaProdT = (float)Math.Sqrt(alphaProdT);
        var sqrtBetaProdT = (float)Math.Sqrt(betaProdT);

        var modelData = modelOutput.Buffer.Span;
        var sampleData = sample.Buffer.Span;
        var result = new float[sampleData.Length];

        // predicted_original_sample = (sample - sqrt(1-alpha_t) * model_output) / sqrt(alpha_t)
        var predOrigSample = new float[sampleData.Length];
        for (int i = 0; i < sampleData.Length; i++)
        {
            predOrigSample[i] = (sampleData[i] - sqrtBetaProdT * modelData[i]) / sqrtAlphaProdT;
        }

        // Clamp predicted original sample
        for (int i = 0; i < predOrigSample.Length; i++)
        {
            predOrigSample[i] = Math.Clamp(predOrigSample[i], -1f, 1f);
        }

        // Compute previous sample: x_{t-1} = sqrt(alpha_{t-1}) * pred_x0 + sqrt(1-alpha_{t-1}) * noise_pred
        var sqrtAlphaProdTPrev = (float)Math.Sqrt(alphaProdTPrev);
        var sqrtBetaProdTPrev = (float)Math.Sqrt(betaProdTPrev);

        // Compute direction pointing to x_t
        for (int i = 0; i < result.Length; i++)
        {
            var predEpsilon = (sampleData[i] - sqrtAlphaProdT * predOrigSample[i]) / sqrtBetaProdT;
            result[i] = sqrtAlphaProdTPrev * predOrigSample[i] + sqrtBetaProdTPrev * predEpsilon;
        }

        return new DenseTensor<float>(result, sample.Dimensions.ToArray());
    }

    private static List<float> Linspace(float start, float end, int count)
    {
        if (count == 1) return new List<float> { start };
        var result = new List<float>(count);
        for (int i = 0; i < count; i++)
            result.Add(start + (end - start) * i / (count - 1));
        return result;
    }
}
