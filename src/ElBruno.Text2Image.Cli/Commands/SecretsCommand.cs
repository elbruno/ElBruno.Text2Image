using System.ComponentModel;
using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Secrets management commands.
/// </summary>
internal sealed class SecretsCommand : AsyncCommand<SecretsCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<action>")]
        [Description("Action: set, list, remove, test")]
        public string Action { get; init; } = string.Empty;

        [CommandArgument(1, "[provider]")]
        [Description("Provider name")]
        public string? Provider { get; init; }
    }

    // TODO(Kaylee): implement secrets set/list/remove/test logic
    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        throw new NotImplementedException("SecretsCommand not yet implemented");
    }
}
