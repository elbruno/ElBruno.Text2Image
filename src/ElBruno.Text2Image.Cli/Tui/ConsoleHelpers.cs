using Spectre.Console;

namespace ElBruno.Text2Image.Cli.Tui;

/// <summary>
/// Helper methods for console output and prompts.
/// </summary>
internal static class ConsoleHelpers
{
    // TODO(Kaylee): implement helper methods for tables, prompts, confirmations, etc.
    public static void PrintProviderTable(IEnumerable<object> providers)
    {
        throw new NotImplementedException();
    }

    public static void PrintSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {message}");
    }

    public static void PrintError(string message)
    {
        AnsiConsole.MarkupLine($"[red]✗[/] {message}");
    }

    public static void PrintWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]⚠[/] {message}");
    }
}
