using System.Diagnostics;
using ElBruno.Text2Image.Schedulers;

namespace ElBruno.Text2Image.Pipeline;

/// <summary>
/// Full Stable Diffusion pipeline: tokenize → encode → denoise → decode → image.
/// Orchestrates all ONNX model components for text-to-image generation.
/// </summary>
internal sealed class StableDiffusionPipeline : IDisposable
{
    private readonly ClipTokenizer _tokenizer;
    private readonly TextEncoder _textEncoder;
    private readonly UNetDenoiser _unet;
    private readonly VaeDecoder _vaeDecoder;
    private readonly Microsoft.ML.OnnxRuntime.SessionOptions _sessionOptions;
    private readonly Microsoft.ML.OnnxRuntime.SessionOptions _cpuOptions;
    private readonly int _embeddingDim;
    private readonly bool _useLcmScheduler;

    public StableDiffusionPipeline(
        string modelPath,
        Microsoft.ML.OnnxRuntime.SessionOptions sessionOptions,
        int embeddingDim = 768,
        bool useLcmScheduler = false)
    {
        _embeddingDim = embeddingDim;
        _useLcmScheduler = useLcmScheduler;
        _sessionOptions = sessionOptions;

        var tokenizerDir = Path.Combine(modelPath, "tokenizer");
        _tokenizer = ClipTokenizer.Load(tokenizerDir);

        var textEncoderPath = Path.Combine(modelPath, "text_encoder", "model.onnx");
        _textEncoder = new TextEncoder(textEncoderPath, sessionOptions);

        var unetPath = Path.Combine(modelPath, "unet", "model.onnx");
        _unet = new UNetDenoiser(unetPath, sessionOptions);

        var vaeDecoderPath = Path.Combine(modelPath, "vae_decoder", "model.onnx");
        // VAE decoder often works best on CPU to avoid OOM
        _cpuOptions = new Microsoft.ML.OnnxRuntime.SessionOptions();
        _cpuOptions.AppendExecutionProvider_CPU();
        _vaeDecoder = new VaeDecoder(vaeDecoderPath, _cpuOptions);
    }

    /// <summary>
    /// Generates an image from a text prompt using the full Stable Diffusion pipeline.
    /// </summary>
    public ImageGenerationResult Generate(
        string prompt,
        ImageGenerationOptions options,
        string modelName)
    {
        var sw = Stopwatch.StartNew();
        var seed = options.Seed ?? Random.Shared.Next();
        var width = options.Width;
        var height = options.Height;

        // 1. Tokenize
        var condTokens = _tokenizer.Tokenize(prompt);
        var uncondTokens = ClipTokenizer.CreateUnconditionalTokens();

        // 2. Text encode
        Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float> textEmbeddings;
        bool useCfg = options.GuidanceScale > 1.0;

        if (useCfg)
        {
            // Standard SD: encode both conditional and unconditional for CFG
            textEmbeddings = _textEncoder.EncodeWithGuidance(condTokens, uncondTokens, _embeddingDim);
        }
        else
        {
            // LCM mode: no CFG needed, just encode the prompt
            textEmbeddings = _textEncoder.Encode(condTokens, _embeddingDim);
        }

        // 3. Create scheduler and set timesteps
        IScheduler scheduler = _useLcmScheduler
            ? new LCMScheduler()
            : new EulerAncestralDiscreteScheduler();
        var timesteps = scheduler.SetTimesteps(options.NumInferenceSteps);

        // 4. Generate initial latent noise
        var latents = TensorHelper.GenerateLatentSample(height, width, seed, scheduler.InitNoiseSigma);

        // 5. Denoising loop
        for (int t = 0; t < timesteps.Length; t++)
        {
            Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float> noisePred;

            if (useCfg)
            {
                // Duplicate latents for CFG: [2, 4, H/8, W/8]
                var latentModelInput = TensorHelper.Duplicate(
                    latents.Buffer.ToArray(),
                    new int[] { 2, 4, height / 8, width / 8 });

                // Scale input
                latentModelInput = scheduler.ScaleInput(latentModelInput, timesteps[t]);

                // Run UNet
                noisePred = _unet.Predict(latentModelInput, timesteps[t], textEmbeddings);

                // Split into unconditional and conditional predictions
                var (noisePredUncond, noisePredText) = TensorHelper.SplitTensor(
                    noisePred, 4, height / 8, width / 8);

                // Apply classifier-free guidance
                noisePred = TensorHelper.ApplyGuidance(noisePredUncond, noisePredText, options.GuidanceScale);
            }
            else
            {
                // LCM: no CFG, single forward pass
                var latentModelInput = scheduler.ScaleInput(latents, timesteps[t]);
                noisePred = _unet.Predict(latentModelInput, timesteps[t], textEmbeddings);
            }

            // Scheduler step
            latents = scheduler.Step(noisePred, timesteps[t], latents);
        }

        // 6. Scale latents and decode with VAE
        latents = TensorHelper.MultiplyByFloat(latents, 1.0f / 0.18215f);
        var imageBytes = _vaeDecoder.Decode(latents, width, height);

        sw.Stop();

        return new ImageGenerationResult
        {
            ImageBytes = imageBytes,
            ModelName = modelName,
            Prompt = prompt,
            Seed = seed,
            InferenceTimeMs = sw.ElapsedMilliseconds,
            Width = width,
            Height = height
        };
    }

    public void Dispose()
    {
        _textEncoder.Dispose();
        _unet.Dispose();
        _vaeDecoder.Dispose();
        _sessionOptions.Dispose();
        _cpuOptions.Dispose();
    }
}
