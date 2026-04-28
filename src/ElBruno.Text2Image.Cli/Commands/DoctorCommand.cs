using System.Reflection;
using System.Runtime.InteropServices;
using Spectre.Console;
using Spectre.Console.Cli;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Cli.Tui;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Diagnostic command — checks GPU availability, config, secret stores.
/// </summary>
internal sealed class DoctorCommand : AsyncCommand
{
    private readonly ProviderRegistry _providerRegistry;
    private readonly SecretResolver _secretResolver;
    private readonly ConfigStore _configStore;

    public DoctorCommand(ProviderRegistry providerRegistry, SecretResolver secretResolver, ConfigStore configStore)
    {
        _providerRegistry = providerRegistry;
        _secretResolver = secretResolver;
        _configStore = configStore;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var ct = CancellationToken.None;
        var allGreen = true;

        AnsiConsole.Write(new Rule("[bold cyan]t2i Doctor — System Diagnostics[/]").RuleStyle("blue").LeftJustified());
        AnsiConsole.WriteLine();

        // 1. CLI Tool info
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        var infoVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? version;
        
        var cliPanel = new Panel(new Rows(
            new Markup($"CLI: [bold cyan]t2i[/] v[cyan]{Markup.Escape(infoVersion)}[/]")
        ))
        {
            Header = new PanelHeader("[bold]CLI Tool[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(cliPanel);
        AnsiConsole.WriteLine();

        // 2. System info
        var systemPanel = new Panel(new Rows(
            new Text($"OS: {RuntimeInformation.OSDescription}"),
            new Text($".NET: {RuntimeInformation.FrameworkDescription}"),
            new Text($"Arch: {RuntimeInformation.ProcessArchitecture}"),
            new Text($"Working Memory: {GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024 * 1024)} GB available")
        ))
        {
            Header = new PanelHeader("[bold]System[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(systemPanel);
        AnsiConsole.WriteLine();

        // 3. Providers
        AnsiConsole.MarkupLine("[bold]Providers[/]");
        var providerTable = new Table().Border(TableBorder.Rounded);
        providerTable.AddColumn("Provider");
        providerTable.AddColumn("Kind");
        providerTable.AddColumn("Status");
        providerTable.AddColumn("Reason");

        foreach (var provider in _providerRegistry.All)
        {
            var health = await provider.CheckAsync(ct);
            var status = health.Ok ? "[green]✓ Available[/]" : "[red]✗ Unavailable[/]";
            var reason = health.Reason ?? "";
            
            if (!health.Ok)
                allGreen = false;

            providerTable.AddRow(
                Markup.Escape(provider.DisplayName),
                provider.Kind.ToString(),
                status,
                Markup.Escape(reason));
        }
        AnsiConsole.Write(providerTable);
        AnsiConsole.WriteLine();

        // 4. Config
        var configExists = File.Exists(ConfigPaths.ConfigFilePath);
        var configStatus = configExists ? "[green]✓ Present[/]" : "[yellow]⚠ Not found[/]";
        var configSize = configExists ? $"{new FileInfo(ConfigPaths.ConfigFilePath).Length} bytes" : "—";

        var configPanel = new Panel(new Rows(
            new Markup($"Path: [cyan]{Markup.Escape(ConfigPaths.ConfigFilePath)}[/]"),
            new Markup($"Status: {configStatus}"),
            new Text($"Size: {configSize}")
        ))
        {
            Header = new PanelHeader("[bold]Configuration[/]"),
            Border = BoxBorder.Rounded
        };
        AnsiConsole.Write(configPanel);
        AnsiConsole.WriteLine();

        // 5. Secrets and Fields
        AnsiConsole.MarkupLine("[bold]Configuration Status[/]");
        var configTable = new Table().Border(TableBorder.Rounded);
        configTable.AddColumn("Provider");
        configTable.AddColumn("Field");
        configTable.AddColumn("Type");
        configTable.AddColumn("Status");

        var hasConfigItems = false;
        
        // Check RequiredFields
        var currentConfig = await _configStore.LoadAsync(ct);
        foreach (var provider in _providerRegistry.All.Where(p => p.RequiredFields.Count > 0))
        {
            var providerCfg = currentConfig.Providers.GetValueOrDefault(provider.Id);
            foreach (var field in provider.RequiredFields)
            {
                hasConfigItems = true;
                var value = field.Equals("endpoint", StringComparison.OrdinalIgnoreCase) ? providerCfg?.Endpoint
                          : field.Equals("model", StringComparison.OrdinalIgnoreCase) ? providerCfg?.Model
                          : null;
                var status = value != null ? "[green]✓ Configured[/]" : "[red]✗ Missing[/]";
                
                if (value == null)
                    allGreen = false;

                configTable.AddRow(
                    Markup.Escape(provider.Id),
                    Markup.Escape(field),
                    "config",
                    status);
            }
        }
        
        // Check RequiredSecrets
        foreach (var provider in _providerRegistry.All.Where(p => p.RequiredSecrets.Count > 0))
        {
            foreach (var field in provider.RequiredSecrets)
            {
                hasConfigItems = true;
                var value = await _secretResolver.ResolveAsync(provider.Id, field, null, ct);
                var status = value != null ? "[green]✓ Configured[/]" : "[red]✗ Missing[/]";
                
                if (value == null)
                    allGreen = false;

                configTable.AddRow(
                    Markup.Escape(provider.Id),
                    Markup.Escape(field),
                    "secret",
                    status);
            }
        }

        if (hasConfigItems)
        {
            AnsiConsole.Write(configTable);
        }
        else
        {
            ConsoleHelpers.PrintInfo("No cloud providers require configuration.");
        }

        AnsiConsole.WriteLine();

        // Summary
        if (allGreen)
        {
            ConsoleHelpers.PrintSuccess("All checks passed!");
            return 0;
        }
        else
        {
            ConsoleHelpers.PrintWarning("Some checks failed. See details above.");
            return 1;
        }
    }
}
