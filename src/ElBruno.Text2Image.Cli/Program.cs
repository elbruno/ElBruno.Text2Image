using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using ElBruno.Text2Image.Cli.Commands;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Infrastructure;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;

var services = new ServiceCollection();

// Register core services
services.AddSingleton<ConfigStore>();
services.AddSingleton<SecretResolver>();

// Register all provider adapters
services.AddSingleton<IProviderAdapter, LocalCpuAdapter>();
services.AddSingleton<IProviderAdapter, LocalCudaAdapter>();
services.AddSingleton<IProviderAdapter, LocalDirectMlAdapter>();
services.AddSingleton<IProviderAdapter, FoundryFlux2Adapter>();
services.AddSingleton<IProviderAdapter, FoundryMaiImage2Adapter>();

// Register provider registry (depends on all IProviderAdapter instances)
services.AddSingleton(sp => new ProviderRegistry(sp.GetServices<IProviderAdapter>()));

// Register secret stores
services.AddSingleton<ISecretStore, EnvVarSecretStore>();
services.AddSingleton<ISecretStore, DpapiSecretStore>();
services.AddSingleton<ISecretStore, PlainFileSecretStore>();

// Create the Spectre.Console.Cli app with DI integration
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("t2i");

    // Default command (prompt positional arg)
    config.AddCommand<GenerateCommand>("generate")
        .WithAlias("gen")
        .WithDescription("Generate an image from a text prompt")
        .WithExample(new[] { "generate", "\"a cat\"" })
        .WithExample(new[] { "generate", "\"a mountain landscape\"", "--provider", "foundry-flux2", "--width", "1024", "--height", "1024" });

    // Config commands
    config.AddCommand<ConfigCommand>("config")
        .WithDescription("Manage configuration")
        .WithExample(new[] { "config", "show" })
        .WithExample(new[] { "config", "set", "default-provider", "cpu" })
        .WithExample(new[] { "config", "path" });

    // Secrets commands
    config.AddCommand<SecretsCommand>("secrets")
        .WithDescription("Manage provider secrets")
        .WithExample(new[] { "secrets", "set", "foundry-flux2" })
        .WithExample(new[] { "secrets", "list" })
        .WithExample(new[] { "secrets", "test", "foundry-flux2" });

    // Doctor command
    config.AddCommand<DoctorCommand>("doctor")
        .WithDescription("Run diagnostics on providers and configuration")
        .WithExample(new[] { "doctor" });

    // Providers command
    config.AddCommand<ProvidersCommand>("providers")
        .WithDescription("List available providers and their status")
        .WithExample(new[] { "providers" });

    // Version command
    config.AddCommand<VersionCommand>("version")
        .WithDescription("Display version information")
        .WithExample(new[] { "version" });
});

return await app.RunAsync(args);
