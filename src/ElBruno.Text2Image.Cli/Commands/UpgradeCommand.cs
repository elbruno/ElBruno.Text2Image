using System.ComponentModel;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Refreshes existing t2i skill files without creating or modifying unrelated files.
/// </summary>
internal sealed class UpgradeCommand : Command<UpgradeCommand.Settings>
{
    private const string ManagedSkillMarker = "<!-- t2i:managed-skill -->";
    private const string LegacySkillHeader = "# t2i — Text-to-Image CLI Skill";

    public sealed class Settings : CommandSettings
    {
        [CommandOption("--target <TARGET>")]
        [Description("Target platform(s): github, claude, or all (default: all)")]
        [DefaultValue("all")]
        public string Target { get; set; } = "all";
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var skillContent = LoadEmbeddedSkillContent();
        if (skillContent == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to load embedded SKILL.md resource");
            return 1;
        }

        var targets = GetTargets(settings.Target);
        if (targets.Count == 0)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] Invalid target '{Markup.Escape(settings.Target)}'. Valid values: github, claude, all");
            return 1;
        }

        var cwd = Directory.GetCurrentDirectory();
        var managedSkillCount = 0;
        var errorCount = 0;

        foreach (var target in targets)
        {
            var relativePath = GetRelativePath(target);
            var fullPath = Path.GetFullPath(Path.Combine(cwd, relativePath));

            if (!File.Exists(fullPath))
            {
                AnsiConsole.MarkupLineInterpolated($"[dim]•[/] {Markup.Escape(relativePath)} [dim]not found[/]");
                continue;
            }

            string existingContent;
            try
            {
                existingContent = File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                errorCount++;
                AnsiConsole.MarkupLineInterpolated($"[red]✗[/] {Markup.Escape(relativePath)} [red]error: {Markup.Escape(ex.Message)}[/]");
                continue;
            }

            if (!IsManagedSkill(existingContent))
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]•[/] {Markup.Escape(relativePath)} [yellow]skipped (not a managed t2i skill)[/]");
                continue;
            }

            managedSkillCount++;
            if (string.Equals(existingContent, skillContent, StringComparison.Ordinal))
            {
                AnsiConsole.MarkupLineInterpolated($"[dim]•[/] {Markup.Escape(relativePath)} [dim]current[/]");
                continue;
            }

            if (ReplaceSkillFile(fullPath, skillContent, out var error))
            {
                AnsiConsole.MarkupLineInterpolated($"[green]✓[/] {Markup.Escape(relativePath)} [green]updated[/]");
            }
            else
            {
                errorCount++;
                AnsiConsole.MarkupLineInterpolated($"[red]✗[/] {Markup.Escape(relativePath)} [red]error: {Markup.Escape(error!)}[/]");
            }
        }

        if (managedSkillCount == 0)
        {
            AnsiConsole.MarkupLine("[dim]No managed t2i skill files found to upgrade.[/]");
        }

        return errorCount == 0 ? 0 : 1;
    }

    private static string? LoadEmbeddedSkillContent()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "ElBruno.Text2Image.Cli.Skills.SKILL.md";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static List<string> GetTargets(string target)
    {
        return target.ToLowerInvariant() switch
        {
            "github" => new List<string> { "github" },
            "claude" => new List<string> { "claude" },
            "all" => new List<string> { "github", "claude" },
            _ => new List<string>()
        };
    }

    private static string GetRelativePath(string target)
    {
        return target switch
        {
            "github" => Path.Combine(".github", "skills", "t2i", "SKILL.md"),
            "claude" => Path.Combine(".claude", "skills", "t2i", "SKILL.md"),
            _ => throw new ArgumentException($"Unknown target: {target}")
        };
    }

    private static bool IsManagedSkill(string content)
    {
        return content.Contains(ManagedSkillMarker, StringComparison.Ordinal) ||
               (content.StartsWith("---", StringComparison.Ordinal) &&
                content.Contains("name: t2i", StringComparison.Ordinal) &&
                content.Contains(LegacySkillHeader, StringComparison.Ordinal));
    }

    private static bool ReplaceSkillFile(string fullPath, string content, out string? error)
    {
        var temporaryPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(temporaryPath, content);
            File.Replace(temporaryPath, fullPath, null);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
