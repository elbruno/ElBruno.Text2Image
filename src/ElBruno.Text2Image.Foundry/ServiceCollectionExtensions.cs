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
    /// <param name="services">The service collection.</param>
    /// <param name="endpoint">The Microsoft Foundry endpoint URL (base URL or full URL).</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="modelName">Optional display name (e.g., "FLUX.2 Pro"). Defaults to "FLUX.2-pro".</param>
    /// <param name="modelId">
    /// The model/deployment name for the API request body (e.g., "FLUX.2-pro", "FLUX.2-flex").
    /// Defaults to "FLUX.2-pro".
    /// </param>
    public static IServiceCollection AddFlux2Generator(
        this IServiceCollection services,
        string endpoint,
        string apiKey,
        string? modelName = null,
        string? modelId = null)
    {
        services.AddSingleton<IImageGenerator>(new Flux2Generator(endpoint, apiKey, modelName, modelId));
        return services;
    }
}
