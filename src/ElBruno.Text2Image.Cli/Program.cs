using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using ElBruno.Text2Image.Cli.Commands;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Infrastructure;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;

var services = new ServiceCollection();

// Register CLI infrastructure (config, secrets)
services.AddCliInfrastructure();

// Register all provider adapters
services.AddCliProviders();

// Create the Spectre.Console.Cli app with DI integration
var registrar = new TypeRegistrar(services);
var app = new CommandApp<GenerateCommand>(registrar);

app.Configure(config =>
{
    config.SetApplicationName("t2i");

    // Config commands
    config.AddCommand<ConfigCommand>("config")
        .WithDescription("Manage configuration")
        .WithExample(new[] { "config", "show" })
        .WithExample(new[] { "config", "set", "foundry-flux2.endpoint", "https://..." })
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
