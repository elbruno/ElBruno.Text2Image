using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Cli.Tui;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Configuration management commands.
/// </summary>
internal sealed class ConfigCommand : AsyncCommand<ConfigCommand.Settings>
{
    private readonly ProviderRegistry _providerRegistry;
    private readonly SecretResolver _secretResolver;
    private readonly ConfigStore _configStore;

    public ConfigCommand(ProviderRegistry providerRegistry, SecretResolver secretResolver, ConfigStore configStore)
    {
        _providerRegistry = providerRegistry;
        _secretResolver = secretResolver;
        _configStore = configStore;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[action]")]
        [Description("Action: show, set, set-all, remove, path")]
        public string? Action { get; init; }

        [CommandArgument(1, "[key]")]
        [Description("Configuration key (e.g., provider.endpoint)")]
        public string? Key { get; init; }

        [CommandArgument(2, "[value]")]
        [Description("Configuration value")]
        public string? Value { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ct = CancellationToken.None;

        // No action → launch wizard
        if (string.IsNullOrWhiteSpace(settings.Action))
        {
            await SetupWizard.RunAsync(_providerRegistry, _secretResolver, _configStore, ct);
            return 0;
        }

        return settings.Action.ToLowerInvariant() switch
        {
            "show" => await ShowAsync(ct),
            "set" => await SetAsync(settings.Key, settings.Value, ct),
            "set-all" => await SetAllAsync(settings.Key, settings.Value, ct),
            "remove" => await RemoveAsync(settings.Key, ct),
            "path" => ShowPath(),
            _ => InvalidAction(settings.Action)
        };
    }

    private async Task<int> ShowAsync(CancellationToken ct)
    {
        var config = await _configStore.LoadAsync(ct);
        
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Provider");
        table.AddColumn("Field");
        table.AddColumn("Source");
        table.AddColumn("Value");

        // Default provider
        if (config.DefaultProvider != null)
        {
            table.AddRow(
                "[bold](default)[/]",
                "provider",
                "config",
                Markup.Escape(config.DefaultProvider));
        }

        // Per-provider config
        foreach (var (providerId, providerCfg) in config.Providers)
        {
            if (providerCfg.Endpoint != null)
            {
                table.AddRow(Markup.Escape(providerId), "endpoint", "config", Markup.Escape(providerCfg.Endpoint));
            }
            if (providerCfg.Model != null)
            {
                table.AddRow(Markup.Escape(providerId), "model", "config", Markup.Escape(providerCfg.Model));
            }
        }

        // Required fields (not in config) — show as "[grey](not set)[/]"
        foreach (var provider in _providerRegistry.All)
        {
            var providerCfg = config.Providers.GetValueOrDefault(provider.Id);
            foreach (var field in provider.RequiredFields)
            {
                var hasValue = field.Equals("endpoint", StringComparison.OrdinalIgnoreCase) ? providerCfg?.Endpoint != null
                             : field.Equals("model", StringComparison.OrdinalIgnoreCase) ? providerCfg?.Model != null
                             : false;
                
                if (!hasValue)
                {
                    table.AddRow(Markup.Escape(provider.Id), Markup.Escape(field), "config", "[grey](not set)[/]");
                }
            }
        }

        // Secrets (using InspectAsync)
        foreach (var provider in _providerRegistry.All)
        {
            foreach (var field in provider.RequiredSecrets)
            {
                var value = await _secretResolver.ResolveAsync(provider.Id, field, null, ct);
                if (value != null)
                {
                    var masked = ConsoleHelpers.Mask(value);
                    table.AddRow(Markup.Escape(provider.Id), Markup.Escape(field), "secret", Markup.Escape(masked));
                }
                else
                {
                    table.AddRow(Markup.Escape(provider.Id), Markup.Escape(field), "secret", "[grey](not set)[/]");
                }
            }
        }

        AnsiConsole.Write(table);
        return 0;
    }

    private async Task<int> SetAsync(string? key, string? value, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            ConsoleHelpers.PrintError("Usage: t2i config set <provider>.<field> <value>");
            return 1;
        }

        // Handle global config keys (no dot-separated provider prefix)
        if (key.Equals("default-provider", StringComparison.OrdinalIgnoreCase))
        {
            var globalConfig = await _configStore.LoadAsync(ct);
            globalConfig.DefaultProvider = value;
            await _configStore.SaveAsync(globalConfig, ct);
            ConsoleHelpers.PrintSuccess($"Default provider set to '{value}'");
            return 0;
        }

        var parts = key.Split('.', 2);
        if (parts.Length != 2)
        {
            ConsoleHelpers.PrintError("Key must be in format: <provider>.<field>");
            return 1;
        }

        var providerId = parts[0];
        var field = parts[1];

        var config = await _configStore.LoadAsync(ct);

        // Check if it's a secret field
        var provider = _providerRegistry.Get(providerId);
        if (provider != null && provider.RequiredSecrets.Contains(field, StringComparer.OrdinalIgnoreCase))
        {
            await _secretResolver.SetAsync(providerId, field, value, ct);
            ConsoleHelpers.PrintSuccess($"Secret '{field}' set for provider '{providerId}'");
        }
        else if (provider != null && provider.RequiredFields.Contains(field, StringComparer.OrdinalIgnoreCase))
        {
            // RequiredFields (endpoint, model) go to ProviderConfig
            if (!config.Providers.ContainsKey(providerId))
            {
                config.Providers[providerId] = new ProviderConfig();
            }

            var providerCfg = config.Providers[providerId];
            
            if (field.Equals("endpoint", StringComparison.OrdinalIgnoreCase))
            {
                providerCfg.Endpoint = value;
            }
            else if (field.Equals("model", StringComparison.OrdinalIgnoreCase))
            {
                providerCfg.Model = value;
            }

            await _configStore.SaveAsync(config, ct);
            ConsoleHelpers.PrintSuccess($"Config '{field}' set for provider '{providerId}'");
        }
        else
        {
            // Non-secret, non-required config field (endpoint, model, etc.)
            if (!config.Providers.ContainsKey(providerId))
            {
                config.Providers[providerId] = new ProviderConfig();
            }

            var providerCfg = config.Providers[providerId];
            
            if (field.Equals("endpoint", StringComparison.OrdinalIgnoreCase))
            {
                providerCfg.Endpoint = value;
            }
            else if (field.Equals("model", StringComparison.OrdinalIgnoreCase))
            {
                providerCfg.Model = value;
            }
            else
            {
                providerCfg.Extras[field] = value;
            }

            await _configStore.SaveAsync(config, ct);
            ConsoleHelpers.PrintSuccess($"Config '{field}' set for provider '{providerId}'");
        }

        return 0;
    }

    private async Task<int> SetAllAsync(string? field, string? value, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(value))
        {
            ConsoleHelpers.PrintError("Usage: t2i config set-all <field> <value>  (e.g., set-all apiKey <key>)");
            return 1;
        }

        var cloudProviders = _providerRegistry.All
            .Where(p => p.Kind == ProviderKind.Cloud)
            .ToList();

        if (cloudProviders.Count == 0)
        {
            ConsoleHelpers.PrintError("No cloud providers are registered.");
            return 1;
        }

        var config = await _configStore.LoadAsync(ct);
        var updated = new List<string>();

        foreach (var provider in cloudProviders)
        {
            if (provider.RequiredSecrets.Contains(field, StringComparer.OrdinalIgnoreCase))
            {
                await _secretResolver.SetAsync(provider.Id, field, value, ct);
                updated.Add(provider.Id);
            }
            else if (provider.RequiredFields.Contains(field, StringComparer.OrdinalIgnoreCase) ||
                     field.Equals("endpoint", StringComparison.OrdinalIgnoreCase) ||
                     field.Equals("model", StringComparison.OrdinalIgnoreCase))
            {
                if (!config.Providers.ContainsKey(provider.Id))
                {
                    config.Providers[provider.Id] = new ProviderConfig();
                }

                var providerCfg = config.Providers[provider.Id];
                if (field.Equals("endpoint", StringComparison.OrdinalIgnoreCase))
                {
                    providerCfg.Endpoint = value;
                }
                else if (field.Equals("model", StringComparison.OrdinalIgnoreCase))
                {
                    providerCfg.Model = value;
                }
                else
                {
                    providerCfg.Extras[field] = value;
                }

                updated.Add(provider.Id);
            }
            else
            {
                // Unknown field — store as an extra so it still applies uniformly.
                if (!config.Providers.ContainsKey(provider.Id))
                {
                    config.Providers[provider.Id] = new ProviderConfig();
                }
                config.Providers[provider.Id].Extras[field] = value;
                updated.Add(provider.Id);
            }
        }

        await _configStore.SaveAsync(config, ct);

        ConsoleHelpers.PrintSuccess($"Set '{field}' for {updated.Count} cloud provider(s): {string.Join(", ", updated)}");
        return 0;
    }

    private async Task<int> RemoveAsync(string? key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            ConsoleHelpers.PrintError("Usage: t2i config remove <provider>");
            return 1;
        }

        // Handle global config keys (no interactive prompt needed)
        if (key.Equals("default-provider", StringComparison.OrdinalIgnoreCase))
        {
            var cfg = await _configStore.LoadAsync(ct);
            cfg.DefaultProvider = null;
            await _configStore.SaveAsync(cfg, ct);
            ConsoleHelpers.PrintSuccess("Removed default provider setting");
            return 0;
        }

        var providerId = key;

        if (!AnsiConsole.Confirm($"Remove all config and secrets for provider '{providerId}'?", defaultValue: false))
        {
            ConsoleHelpers.PrintInfo("Cancelled.");
            return 0;
        }

        var config = await _configStore.LoadAsync(ct);
        config.Providers.Remove(providerId);
        await _configStore.SaveAsync(config, ct);

        // Also remove secrets
        var provider = _providerRegistry.Get(providerId);
        if (provider != null)
        {
            foreach (var field in provider.RequiredSecrets)
            {
                await _secretResolver.DeleteAsync(providerId, field, ct);
            }
        }

        ConsoleHelpers.PrintSuccess($"Removed config and secrets for provider '{providerId}'");
        return 0;
    }

    private int ShowPath()
    {
        AnsiConsole.MarkupLineInterpolated($"[cyan]{Markup.Escape(ConfigPaths.ConfigFilePath)}[/]");
        return 0;
    }

    private int InvalidAction(string action)
    {
        ConsoleHelpers.PrintError($"Unknown action: {action}");
        AnsiConsole.MarkupLine("Valid actions: [cyan]show[/], [cyan]set[/], [cyan]set-all[/], [cyan]remove[/], [cyan]path[/]");
        return 1;
    }
}
