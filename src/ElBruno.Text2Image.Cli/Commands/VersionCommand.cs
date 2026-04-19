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
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        
        // Try to get informational version (includes build SHA if available)
        var infoVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? version;

        AnsiConsole.MarkupLineInterpolated($"[bold]t2i[/] version [cyan]{Markup.Escape(infoVersion)}[/]");
        return 0;
    }
}
