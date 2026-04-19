using Microsoft.Extensions.DependencyInjection;
using ElBruno.Text2Image.Cli.Providers;

namespace ElBruno.Text2Image.Cli.Infrastructure;

/// <summary>
/// DI extensions for registering provider adapters.
/// </summary>
public static class ProviderServiceCollectionExtensions
{
    /// <summary>
    /// Registers all provider adapters and the provider registry.
    /// </summary>
    public static IServiceCollection AddCliProviders(this IServiceCollection services)
    {
        services.AddHttpClient();
        
        services.AddSingleton<IProviderAdapter, LocalCpuAdapter>();
        services.AddSingleton<IProviderAdapter, LocalCudaAdapter>();
        services.AddSingleton<IProviderAdapter, LocalDirectMlAdapter>();
        services.AddSingleton<IProviderAdapter, FoundryFlux2Adapter>();
        services.AddSingleton<IProviderAdapter, FoundryMaiImage2Adapter>();
        
        services.AddSingleton<ProviderRegistry>();
        
        return services;
    }
}
