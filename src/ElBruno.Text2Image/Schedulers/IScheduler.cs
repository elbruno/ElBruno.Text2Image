using Microsoft.ML.OnnxRuntime.Tensors;

namespace ElBruno.Text2Image.Schedulers;

/// <summary>
/// Interface for diffusion noise schedulers used in the denoising loop.
/// </summary>
internal interface IScheduler
{
    /// <summary>
    /// The initial noise sigma used to scale the latent sample.
    /// </summary>
    float InitNoiseSigma { get; }

    /// <summary>
    /// The computed timesteps for the inference loop.
    /// </summary>
    List<int> Timesteps { get; }

    /// <summary>
    /// Sets up the timestep schedule for the given number of inference steps.
    /// </summary>
    int[] SetTimesteps(int numInferenceSteps);

    /// <summary>
    /// Scales the model input according to the current timestep (for schedulers that need it).
    /// </summary>
    DenseTensor<float> ScaleInput(DenseTensor<float> sample, int timestep);

    /// <summary>
    /// Performs a single scheduler step: takes model output and produces the previous sample.
    /// </summary>
    DenseTensor<float> Step(DenseTensor<float> modelOutput, int timestep, DenseTensor<float> sample, int order = 4);
}
