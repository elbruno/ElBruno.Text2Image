using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Diagnostic command — checks GPU availability, config, secret stores.
/// </summary>
internal sealed class DoctorCommand : AsyncCommand
{
    // TODO(Kaylee): implement diagnostic checks
    public override Task<int> ExecuteAsync(CommandContext context)
    {
        throw new NotImplementedException("DoctorCommand not yet implemented");
    }
}
