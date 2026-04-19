using System.Text;
using Spectre.Console;

namespace ElBruno.Text2Image.Cli.Tui;

/// <summary>
/// Helper methods for console output and prompts.
/// </summary>
internal static class ConsoleHelpers
{
    /// <summary>
    /// Checks if the console is interactive (both input and output are TTY).
    /// </summary>
    public static bool IsInteractive()
    {
        return !Console.IsInputRedirected && !Console.IsOutputRedirected
            && AnsiConsole.Profile.Capabilities.Interactive;
    }

    /// <summary>
    /// Masks a secret string for safe display (e.g., "sk-XXX***...***abcd").
    /// </summary>
    public static string Mask(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return "****";

        if (secret.Length <= 8)
            return new string('*', secret.Length);

        var prefix = secret.Length >= 6 ? secret[..6] : secret[..3];
        var suffix = secret.Length >= 4 ? secret[^4..] : "";
        return $"{prefix}***...***{suffix}";
    }

    /// <summary>
    /// Converts text to a filename-safe slug (for default output paths).
    /// </summary>
    public static string Slug(string text, int max = 60)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "output";

        var sb = new StringBuilder();
        foreach (var c in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (char.IsWhiteSpace(c) || c == '-' || c == '_')
                sb.Append('-');
        }

        var slug = sb.ToString().Trim('-');
        if (slug.Length > max)
            slug = slug[..max].TrimEnd('-');

        return string.IsNullOrWhiteSpace(slug) ? "output" : slug;
    }

    public static void PrintSuccess(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"[green]✓[/] {Markup.Escape(message)}");
    }

    public static void PrintError(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"[red]✗[/] {Markup.Escape(message)}");
    }

    public static void PrintWarning(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"[yellow]⚠[/] {Markup.Escape(message)}");
    }

    public static void PrintInfo(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"[blue]ℹ[/] {Markup.Escape(message)}");
    }
}
