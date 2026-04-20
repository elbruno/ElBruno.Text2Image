using Spectre.Console;
using Spectre.Console.Cli;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Cli.Tui;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Lists available providers and their health status.
/// </summary>
internal sealed class ProvidersCommand : AsyncCommand
{
    private readonly ProviderRegistry _providerRegistry;
    private readonly SecretResolver _secretResolver;
    private readonly ConfigStore _configStore;

    public ProvidersCommand(ProviderRegistry providerRegistry, SecretResolver secretResolver, ConfigStore configStore)
    {
        _providerRegistry = providerRegistry;
        _secretResolver = secretResolver;
        _configStore = configStore;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var ct = CancellationToken.None;

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Kind");
        table.AddColumn("Status");
        table.AddColumn("Config Status");

        var config = await _configStore.LoadAsync(ct);

        foreach (var provider in _providerRegistry.All.OrderBy(p => p.Kind).ThenBy(p => p.Id))
        {
            var health = await provider.CheckAsync(ct);
            var status = health.Ok ? "[green]✓[/]" : "[red]✗[/]";
            
            var configStatus = "—";
            var totalRequired = provider.RequiredSecrets.Count + provider.RequiredFields.Count;
            if (totalRequired > 0)
            {
                var configuredCount = 0;
                
                // Check RequiredFields
                var providerCfg = config.Providers.GetValueOrDefault(provider.Id);
                foreach (var field in provider.RequiredFields)
                {
                    var value = field.Equals("endpoint", StringComparison.OrdinalIgnoreCase) ? providerCfg?.Endpoint
                              : field.Equals("model", StringComparison.OrdinalIgnoreCase) ? providerCfg?.Model
                              : null;
                    if (value != null)
                        configuredCount++;
                }
                
                // Check RequiredSecrets
                foreach (var field in provider.RequiredSecrets)
                {
                    var value = await _secretResolver.ResolveAsync(provider.Id, field, null, ct);
                    if (value != null)
                        configuredCount++;
                }

                configStatus = configuredCount == totalRequired
                    ? $"[green]{configuredCount}/{totalRequired}[/]"
                    : $"[yellow]{configuredCount}/{totalRequired}[/]";
            }

            table.AddRow(
                Markup.Escape(provider.Id),
                Markup.Escape(provider.DisplayName),
                provider.Kind.ToString(),
                status,
                configStatus);
        }

        AnsiConsole.Write(table);
        
        AnsiConsole.WriteLine();
        ConsoleHelpers.PrintInfo("Run 't2i config' to configure cloud providers.");
        ConsoleHelpers.PrintInfo("Run 't2i doctor' for detailed diagnostics.");

        return 0;
    }
}
