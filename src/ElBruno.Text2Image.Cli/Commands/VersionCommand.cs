using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Displays version information.
/// </summary>
internal sealed class VersionCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetName()
            .Version?.ToString() ?? "unknown";

        AnsiConsole.MarkupLine($"[bold]t2i[/] version [cyan]{version}[/]");
        return 0;
    }
}
