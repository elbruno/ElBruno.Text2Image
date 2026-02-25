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
}

