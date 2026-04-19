using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Cli.Tui;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Secrets management commands.
/// </summary>
internal sealed class SecretsCommand : AsyncCommand<SecretsCommand.Settings>
{
    private readonly ProviderRegistry _providerRegistry;
    private readonly SecretResolver _secretResolver;

    public SecretsCommand(ProviderRegistry providerRegistry, SecretResolver secretResolver)
    {
        _providerRegistry = providerRegistry;
        _secretResolver = secretResolver;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<action>")]
        [Description("Action: set, list, remove, test")]
        public string Action { get; init; } = string.Empty;

        [CommandArgument(1, "[provider]")]
        [Description("Provider name")]
        public string? Provider { get; init; }

        [CommandOption("--field")]
        [Description("Specific field to remove (optional)")]
        public string? Field { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ct = CancellationToken.None;

        return settings.Action.ToLowerInvariant() switch
        {
            "set" => await SetAsync(settings.Provider, ct),
            "list" => await ListAsync(ct),
            "remove" => await RemoveAsync(settings.Provider, settings.Field, ct),
            "test" => await TestAsync(settings.Provider, ct),
            _ => InvalidAction(settings.Action)
        };
    }

    private async Task<int> SetAsync(string? providerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            ConsoleHelpers.PrintError("Usage: t2i secrets set <provider>");
            return 1;
        }

        var provider = _providerRegistry.Get(providerId);
        if (provider == null)
        {
            ConsoleHelpers.PrintError($"Provider '{providerId}' not found. Run 't2i providers' to list available providers.");
            return 1;
        }

        if (provider.RequiredSecrets.Count == 0)
        {
            ConsoleHelpers.PrintInfo($"Provider '{providerId}' does not require any secrets.");
            return 0;
        }

        if (!ConsoleHelpers.IsInteractive())
        {
            ConsoleHelpers.PrintError("Setting secrets requires an interactive terminal.");
            return 1;
        }

        AnsiConsole.MarkupLineInterpolated($"\n[bold]Setting secrets for {Markup.Escape(provider.DisplayName)}[/]");

        foreach (var field in provider.RequiredSecrets)
        {
            var value = AnsiConsole.Prompt(
                new TextPrompt<string>($"  {Markup.Escape(field)}:")
                    .Secret('*')
                    .Validate(s => !string.IsNullOrWhiteSpace(s), $"{field} cannot be empty"));

            await _secretResolver.SetAsync(providerId, field, value, ct);
        }

        ConsoleHelpers.PrintSuccess($"Secrets saved for provider '{providerId}'");
        return 0;
    }

    private async Task<int> ListAsync(CancellationToken ct)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Provider");
        table.AddColumn("Field");
        table.AddColumn("Configured");

        foreach (var provider in _providerRegistry.All)
        {
            if (provider.RequiredSecrets.Count == 0)
                continue;

            foreach (var field in provider.RequiredSecrets)
            {
                var value = await _secretResolver.ResolveAsync(provider.Id, field, null, ct);
                var configured = value != null ? "[green]✓[/]" : "[red]✗[/]";
                table.AddRow(Markup.Escape(provider.Id), Markup.Escape(field), configured);
            }
        }

        if (table.Rows.Count == 0)
        {
            ConsoleHelpers.PrintInfo("No cloud providers configured.");
            return 0;
        }

        AnsiConsole.Write(table);
        return 0;
    }

    private async Task<int> RemoveAsync(string? providerId, string? field, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            ConsoleHelpers.PrintError("Usage: t2i secrets remove <provider> [--field <name>]");
            return 1;
        }

        var provider = _providerRegistry.Get(providerId);
        if (provider == null)
        {
            ConsoleHelpers.PrintError($"Provider '{providerId}' not found.");
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(field))
        {
            // Remove specific field
            if (!ConsoleHelpers.IsInteractive() || 
                AnsiConsole.Confirm($"Remove secret '{field}' for provider '{providerId}'?", defaultValue: false))
            {
                await _secretResolver.DeleteAsync(providerId, field, ct);
                ConsoleHelpers.PrintSuccess($"Removed secret '{field}' for provider '{providerId}'");
            }
        }
        else
        {
            // Remove all fields
            if (!ConsoleHelpers.IsInteractive() || 
                AnsiConsole.Confirm($"Remove all secrets for provider '{providerId}'?", defaultValue: false))
            {
                foreach (var fieldName in provider.RequiredSecrets)
                {
                    await _secretResolver.DeleteAsync(providerId, fieldName, ct);
                }
                ConsoleHelpers.PrintSuccess($"Removed all secrets for provider '{providerId}'");
            }
        }

        return 0;
    }

    private async Task<int> TestAsync(string? providerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            ConsoleHelpers.PrintError("Usage: t2i secrets test <provider>");
            return 1;
        }

        var provider = _providerRegistry.Get(providerId);
        if (provider == null)
        {
            ConsoleHelpers.PrintError($"Provider '{providerId}' not found.");
            return 1;
        }

        ProviderHealth health;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Testing {provider.DisplayName}...", async ctx =>
            {
                health = await provider.CheckAsync(ct);
            });

        health = await provider.CheckAsync(ct);

        if (health.Ok)
        {
            ConsoleHelpers.PrintSuccess($"{provider.DisplayName} is available and ready.");
            return 0;
        }
        else
        {
            ConsoleHelpers.PrintError($"{provider.DisplayName} is not available: {health.Reason ?? "unknown error"}");
            return 1;
        }
    }

    private int InvalidAction(string action)
    {
        ConsoleHelpers.PrintError($"Unknown action: {action}");
        AnsiConsole.MarkupLine("Valid actions: [cyan]set[/], [cyan]list[/], [cyan]remove[/], [cyan]test[/]");
        return 1;
    }
}
