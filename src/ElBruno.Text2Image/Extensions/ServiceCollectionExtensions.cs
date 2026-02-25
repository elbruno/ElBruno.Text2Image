using Microsoft.Extensions.DependencyInjection;

namespace ElBruno.Text2Image.Extensions;

/// <summary>
/// Extension methods for registering Text2Image services with DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the ViT-GPT2 image captioner to the service collection.
    /// </summary>
    public static IServiceCollection AddViTGpt2Captioner(
        this IServiceCollection services,
        Action<ImageCaptionerOptions>? configureOptions = null)
    {
        var options = new ImageCaptionerOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton<IImageCaptioner>(new Models.ViTGpt2Captioner(options));
        return services;
    }

    /// <summary>
    /// Adds the BLIP image captioner to the service collection.
    /// </summary>
    public static IServiceCollection AddBlipCaptioner(
        this IServiceCollection services,
        Action<ImageCaptionerOptions>? configureOptions = null)
    {
        var options = new ImageCaptionerOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton<IImageCaptioner>(new Models.BlipCaptioner(options));
        return services;
    }
}
