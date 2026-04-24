using System.ComponentModel;
using System.Diagnostics;
using Spectre.Console;
using Spectre.Console.Cli;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Cli.Tui;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Main image generation command.
/// Usage: t2i "a cat" [--provider foundry-flux2] [--out output.png] [--width 512] [--height 512]
/// </summary>
internal sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
{
    private readonly ProviderRegistry _providerRegistry;
    private readonly SecretResolver _secretResolver;
    private readonly ConfigStore _configStore;

    public GenerateCommand(ProviderRegistry providerRegistry, SecretResolver secretResolver, ConfigStore configStore)
    {
        _providerRegistry = providerRegistry;
        _secretResolver = secretResolver;
        _configStore = configStore;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<prompt>")]
        [Description("The text prompt describing the image to generate")]
        public string Prompt { get; init; } = string.Empty;

        [CommandOption("--provider")]
        [Description("Provider to use (foundry-flux2, foundry-mai2)")]
        public string? Provider { get; init; }

        [CommandOption("--out|-o")]
        [Description("Output file path (default: output.png)")]
        public string? OutputPath { get; init; }

        [CommandOption("--width|-w")]
        [Description("Image width in pixels (default: 512)")]
        [DefaultValue(512)]
        public int Width { get; init; } = 512;

        [CommandOption("--height")]
        [Description("Image height in pixels (default: 512)")]
        [DefaultValue(512)]
        public int Height { get; init; } = 512;

        [CommandOption("--steps|-s")]
        [Description("Number of inference steps (default: 20)")]
        [DefaultValue(20)]
        public int Steps { get; init; } = 20;

        [CommandOption("--endpoint")]
        [Description("Cloud provider endpoint (override config)")]
        public string? Endpoint { get; init; }

        [CommandOption("--api-key")]
        [Description("Cloud provider API key (override secrets)")]
        public string? ApiKey { get; init; }

        [CommandOption("--timeout")]
        [Description("Request timeout in seconds (default: 300)")]
        [DefaultValue(300)]
        public int Timeout { get; init; } = 300;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ct = CancellationToken.None; // Spectre.Console.Cli doesn't expose CancellationToken in CommandContext

        // 1. Resolve provider
        string? providerId = settings.Provider;
        
        if (providerId == null)
        {
            var config = await _configStore.LoadAsync(ct);
            providerId = config.DefaultProvider;
        }

        if (providerId == null)
        {
            if (ConsoleHelpers.IsInteractive())
            {
                AnsiConsole.MarkupLine("[yellow]No default provider configured.[/]");
                providerId = await SetupWizard.RunAsync(_providerRegistry, _secretResolver, _configStore, ct);
                if (providerId == null)
                    return 2;
            }
            else
            {
                ConsoleHelpers.PrintError("No default provider configured. Run 't2i config' to set one, or use --provider.");
                return 2;
            }
        }

        var provider = _providerRegistry.Get(providerId);
        if (provider == null)
        {
            ConsoleHelpers.PrintError($"Provider '{providerId}' not found. Run 't2i providers' to list available providers.");
            return 2;
        }

        // 2. Resolve secrets for cloud providers
        var cliOverrides = new Dictionary<string, string?>();
        if (settings.Endpoint != null)
            cliOverrides["endpoint"] = settings.Endpoint;
        if (settings.ApiKey != null)
            cliOverrides["apiKey"] = settings.ApiKey;

        foreach (var field in provider.RequiredSecrets)
        {
            var value = await _secretResolver.ResolveAsync(providerId, field, cliOverrides, ct);
            if (value == null)
            {
                if (ConsoleHelpers.IsInteractive())
                {
                    AnsiConsole.MarkupLineInterpolated($"[yellow]Missing secret '{Markup.Escape(field)}' for provider '{Markup.Escape(providerId)}'[/]");
                    AnsiConsole.MarkupLine("Launching setup wizard...");
                    await SetupWizard.RunAsync(_providerRegistry, _secretResolver, _configStore, ct);
                    
                    // Retry resolution
                    value = await _secretResolver.ResolveAsync(providerId, field, cliOverrides, ct);
                    if (value == null)
                    {
                        ConsoleHelpers.PrintError($"Still missing secret '{field}' after wizard.");
                        return 2;
                    }
                }
                else
                {
                    ConsoleHelpers.PrintError($"Missing secret '{field}' for provider '{providerId}'.");
                    AnsiConsole.MarkupLineInterpolated($"Set with: [cyan]t2i secrets set {Markup.Escape(providerId)}[/]");
                    AnsiConsole.MarkupLineInterpolated($"OR: [cyan]$env:T2I_{Markup.Escape(providerId.ToUpperInvariant())}_{Markup.Escape(field.ToUpperInvariant())}=...[/]");
                    return 2;
                }
            }
            cliOverrides[field] = value;
        }

        // 3. Prepare output path
        var outputPath = settings.OutputPath;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var slug = ConsoleHelpers.Slug(settings.Prompt);
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            outputPath = $"{slug}-{timestamp}.png";
        }

        // 4. Generate image
        cliOverrides["timeout"] = settings.Timeout.ToString();
        var req = new GenerationRequest(
            settings.Prompt,
            settings.Width,
            settings.Height,
            settings.Steps,
            outputPath,
            cliOverrides);

        GenerationResult result;
        var sw = Stopwatch.StartNew();
        
        try
        {
            result = await ProgressRenderer.RunWithProgressAsync(
                $"Generating: {settings.Prompt}",
                (progress, token) => provider.GenerateAsync(req, progress, token),
                ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            ConsoleHelpers.PrintError($"Generation failed: {ex.Message}");
            return 1;
        }

        sw.Stop();

        // 5. Print success
        AnsiConsole.WriteLine();
        ConsoleHelpers.PrintSuccess($"Saved to {result.OutputPath} ({result.ActualWidth}×{result.ActualHeight}, {result.Duration.TotalSeconds:F1}s)");

        // 6. Print metadata table
        if (result.Metadata.Count > 0)
        {
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Key");
            table.AddColumn("Value");

            foreach (var (key, value) in result.Metadata)
            {
                table.AddRow(Markup.Escape(key), Markup.Escape(value));
            }

            AnsiConsole.Write(table);
        }

        return 0;
    }
}
