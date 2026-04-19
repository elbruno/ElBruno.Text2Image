using Microsoft.Extensions.DependencyInjection;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Secrets;

namespace ElBruno.Text2Image.Cli.Infrastructure;

/// <summary>
/// DI registration for CLI infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCliInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISecretStore, EnvVarSecretStore>();
        services.AddSingleton<ISecretStore, DpapiSecretStore>();
        services.AddSingleton<ISecretStore, PlainFileSecretStore>();
        services.AddSingleton<SecretResolver>();
        services.AddSingleton<ConfigStore>();
        return services;
    }
}
