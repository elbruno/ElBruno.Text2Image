using System.ComponentModel;
using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Configuration management commands.
/// </summary>
internal sealed class ConfigCommand : AsyncCommand<ConfigCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[action]")]
        [Description("Action: show, set, remove, path")]
        public string? Action { get; init; }

        [CommandArgument(1, "[key]")]
        [Description("Configuration key (e.g., provider.endpoint)")]
        public string? Key { get; init; }

        [CommandArgument(2, "[value]")]
        [Description("Configuration value")]
        public string? Value { get; init; }
    }

    // TODO(Kaylee): implement config show/set/remove/path logic
    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        throw new NotImplementedException("ConfigCommand not yet implemented");
    }
}
