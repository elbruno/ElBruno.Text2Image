using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Lists available providers and their health status.
/// </summary>
internal sealed class ProvidersCommand : AsyncCommand
{
    // TODO(Kaylee): implement provider listing and health checks
    public override Task<int> ExecuteAsync(CommandContext context)
    {
        throw new NotImplementedException("ProvidersCommand not yet implemented");
    }
}
