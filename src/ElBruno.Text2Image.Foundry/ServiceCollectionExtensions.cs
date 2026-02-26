using Microsoft.Extensions.DependencyInjection;
using ElBruno.Text2Image;

namespace ElBruno.Text2Image.Foundry.Extensions;

/// <summary>
/// Extension methods for registering Microsoft Foundry generators with DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a FLUX.2 cloud API image generator to the service collection.
    /// Requires a Microsoft Foundry deployment endpoint and API key.
    /// </summary>
    public static IServiceCollection AddFlux2Generator(
        this IServiceCollection services,
        string endpoint,
        string apiKey,
        string? modelName = null)
    {
        services.AddSingleton<IImageGenerator>(new Flux2Generator(endpoint, apiKey, modelName));
        return services;
    }
}
