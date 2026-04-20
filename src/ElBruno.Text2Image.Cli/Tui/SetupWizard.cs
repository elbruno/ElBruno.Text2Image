using Spectre.Console;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;

namespace ElBruno.Text2Image.Cli.Tui;

/// <summary>
/// Interactive first-run setup wizard using Spectre.Console.
/// </summary>
internal static class SetupWizard
{
    public static async Task<string?> RunAsync(
        ProviderRegistry providerRegistry,
        SecretResolver secretResolver,
        ConfigStore configStore,
        CancellationToken ct)
    {
        if (!ConsoleHelpers.IsInteractive())
        {
            ConsoleHelpers.PrintError("Setup wizard requires an interactive terminal.");
            return null;
        }

        AnsiConsole.Write(new Rule("[bold cyan]t2i Setup Wizard[/]").RuleStyle("blue").LeftJustified());
        AnsiConsole.WriteLine();

        // Step 1: Select provider
        var providers = providerRegistry.All.ToList();
        var providerChoices = providers.ToDictionary(
            p => p.Id,
            p => $"{p.DisplayName} ({p.Kind})");

        var selectedProviderId = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Which [green]provider[/] do you want to use?")
                .AddChoices(providerChoices.Keys)
                .UseConverter(id => providerChoices[id]));

        var selectedProvider = providerRegistry.Get(selectedProviderId)!;

        // Step 2: If cloud provider, configure secrets
        if (selectedProvider.Kind == ProviderKind.Cloud && selectedProvider.RequiredSecrets.Count > 0)
        {
            AnsiConsole.MarkupLine($"\n[bold]Configuring {Markup.Escape(selectedProvider.DisplayName)}[/]");
            
            foreach (var field in selectedProvider.RequiredSecrets)
            {
                var value = AnsiConsole.Prompt(
                    new TextPrompt<string>($"  {Markup.Escape(field)}:")
                        .Secret('*')
                        .Validate(s => !string.IsNullOrWhiteSpace(s), $"{field} cannot be empty"));

                await secretResolver.SetAsync(selectedProviderId, field, value, ct);
            }
        }

        // Step 3: If cloud provider, configure required fields
        if (selectedProvider.Kind == ProviderKind.Cloud && selectedProvider.RequiredFields.Count > 0)
        {
            var config = await configStore.LoadAsync(ct);
            if (!config.Providers.ContainsKey(selectedProviderId))
            {
                config.Providers[selectedProviderId] = new ProviderConfig();
            }
            var providerCfg = config.Providers[selectedProviderId];

            foreach (var field in selectedProvider.RequiredFields)
            {
                if (field.Equals("endpoint", StringComparison.OrdinalIgnoreCase))
                {
                    var defaultEndpoint = "https://<your-resource>.services.ai.azure.com";
                    var endpoint = AnsiConsole.Prompt(
                        new TextPrompt<string>($"  Endpoint (default: {defaultEndpoint}):")
                            .AllowEmpty()
                            .Validate(s => string.IsNullOrWhiteSpace(s) || Uri.TryCreate(s, UriKind.Absolute, out _), 
                                     "Endpoint must be a valid URL"));

                    providerCfg.Endpoint = string.IsNullOrWhiteSpace(endpoint) ? defaultEndpoint : endpoint;
                }
                else if (field.Equals("model", StringComparison.OrdinalIgnoreCase))
                {
                    var defaultModel = selectedProviderId switch
                    {
                        "foundry-mai2" => "MAI-Image-2",
                        "foundry-flux2" => "FLUX.2-pro",
                        _ => ""
                    };

                    var model = AnsiConsole.Prompt(
                        new TextPrompt<string>($"  Model name (default: {defaultModel}):")
                            .AllowEmpty());

                    providerCfg.Model = string.IsNullOrWhiteSpace(model) ? defaultModel : model;
                }
            }

            await configStore.SaveAsync(config, ct);
        }

        // Step 4: Test connection
        if (selectedProvider.Kind == ProviderKind.Cloud)
        {
            if (AnsiConsole.Confirm("\n[bold]Test connection?[/]", defaultValue: true))
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Testing connection...", async ctx =>
                    {
                        var health = await selectedProvider.CheckAsync(ct);
                        if (health.Ok)
                        {
                            ConsoleHelpers.PrintSuccess("Connection successful!");
                        }
                        else
                        {
                            ConsoleHelpers.PrintError($"Connection failed: {health.Reason ?? "unknown error"}");
                        }
                    });
            }
        }

        // Step 5: Set as default
        var setDefault = AnsiConsole.Confirm("\n[bold]Set as default provider?[/]", defaultValue: true);
        if (setDefault)
        {
            var config = await configStore.LoadAsync(ct);
            config.DefaultProvider = selectedProviderId;
            await configStore.SaveAsync(config, ct);
            ConsoleHelpers.PrintSuccess($"Default provider set to '{selectedProviderId}'");
        }

        AnsiConsole.WriteLine();
        ConsoleHelpers.PrintSuccess("Setup complete!");
        
        return selectedProviderId;
    }
}
