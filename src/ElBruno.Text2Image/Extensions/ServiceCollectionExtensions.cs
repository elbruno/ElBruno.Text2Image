using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.Text2Image.Extensions;

/// <summary>
/// Extension methods for registering Text2Image services with DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Stable Diffusion 1.5 image generator to the service collection.
    /// </summary>
    public static IServiceCollection AddStableDiffusion15(
        this IServiceCollection services,
        Action<ImageGenerationOptions>? configureOptions = null)
    {
        var options = new ImageGenerationOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton<IImageGenerator>(new Models.StableDiffusion15(options));
        return services;
    }

    /// <summary>
    /// Adds the LCM Dreamshaper v7 image generator to the service collection.
    /// LCM generates images in 2-4 steps with no classifier-free guidance needed.
    /// </summary>
    public static IServiceCollection AddLcmDreamshaperV7(
        this IServiceCollection services,
        Action<ImageGenerationOptions>? configureOptions = null)
    {
        var options = new ImageGenerationOptions
        {
            NumInferenceSteps = 4,
            GuidanceScale = 1.0
        };
        configureOptions?.Invoke(options);
        services.AddSingleton<IImageGenerator>(new Models.LcmDreamshaperV7(options));
        return services;
    }

    /// <summary>
    /// Adds a FLUX.2 cloud API image generator to the service collection.
    /// Requires an Azure AI Foundry deployment endpoint and API key.
    /// </summary>
    public static IServiceCollection AddFlux2Generator(
        this IServiceCollection services,
        string endpoint,
        string apiKey,
        string? modelName = null)
    {
        services.AddSingleton<IImageGenerator>(new Models.Flux2Generator(endpoint, apiKey, modelName));
        return services;
    }
}

