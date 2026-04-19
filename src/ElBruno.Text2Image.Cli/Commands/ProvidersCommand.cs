using Spectre.Console;
using Spectre.Console.Cli;
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

    public ProvidersCommand(ProviderRegistry providerRegistry, SecretResolver secretResolver)
    {
        _providerRegistry = providerRegistry;
        _secretResolver = secretResolver;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var ct = CancellationToken.None;

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Kind");
        table.AddColumn("Status");
        table.AddColumn("Secrets Configured");

        foreach (var provider in _providerRegistry.All.OrderBy(p => p.Kind).ThenBy(p => p.Id))
        {
            var health = await provider.CheckAsync(ct);
            var status = health.Ok ? "[green]✓[/]" : "[red]✗[/]";
            
            var secretsStatus = "—";
            if (provider.RequiredSecrets.Count > 0)
            {
                var configuredCount = 0;
                foreach (var field in provider.RequiredSecrets)
                {
                    var value = await _secretResolver.ResolveAsync(provider.Id, field, null, ct);
                    if (value != null)
                        configuredCount++;
                }

                secretsStatus = configuredCount == provider.RequiredSecrets.Count
                    ? $"[green]{configuredCount}/{provider.RequiredSecrets.Count}[/]"
                    : $"[yellow]{configuredCount}/{provider.RequiredSecrets.Count}[/]";
            }

            table.AddRow(
                Markup.Escape(provider.Id),
                Markup.Escape(provider.DisplayName),
                provider.Kind.ToString(),
                status,
                secretsStatus);
        }

        AnsiConsole.Write(table);
        
        AnsiConsole.WriteLine();
        ConsoleHelpers.PrintInfo("Run 't2i secrets set <provider>' to configure cloud providers.");
        ConsoleHelpers.PrintInfo("Run 't2i doctor' for detailed diagnostics.");

        return 0;
    }
}
