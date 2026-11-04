using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Refreshes existing t2i skill files without creating or modifying unrelated files.
/// </summary>
internal sealed class UpgradeCommand : Command<UpgradeCommand.Settings>
{
    private const string ManagedSkillMarker = "<!-- t2i:managed-skill -->";
    private const string ManagedSkillReferenceMarker = "<!-- t2i:managed-skill-reference -->";
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
        var skillFiles = SkillFileResources.Load();
        if (skillFiles == null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to load embedded skill files");
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
            var relativeDirectory = GetRelativeDirectory(target);
            var fullDirectory = Path.GetFullPath(Path.Combine(cwd, relativeDirectory));
            var relativePath = Path.Combine(relativeDirectory, "SKILL.md");
            var fullPath = Path.Combine(fullDirectory, "SKILL.md");

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
            foreach (var (assetPath, content) in skillFiles.OrderBy(file => file.Key == "SKILL.md" ? 1 : 0))
                errorCount += RefreshSkillFile(fullDirectory, relativeDirectory, assetPath, content, existingContent);
        }

        if (managedSkillCount == 0)
        {
            AnsiConsole.MarkupLine("[dim]No managed t2i skill files found to upgrade.[/]");
        }

        return errorCount == 0 ? 0 : 1;
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

    private static string GetRelativeDirectory(string target)
    {
        return target switch
        {
            "github" => Path.Combine(".github", "skills", "t2i"),
            "claude" => Path.Combine(".claude", "skills", "t2i"),
            _ => throw new ArgumentException($"Unknown target: {target}")
        };
    }

    private static int RefreshSkillFile(
        string fullDirectory,
        string relativeDirectory,
        string assetPath,
        string content,
        string existingMainContent)
    {
        var relativePath = Path.Combine(relativeDirectory, assetPath);
        var fullPath = Path.GetFullPath(Path.Combine(fullDirectory, assetPath));
        if (!IsWithinDirectory(fullDirectory, fullPath))
        {
            AnsiConsole.MarkupLineInterpolated($"[red]✗[/] {Markup.Escape(relativePath)} [red]error: invalid skill path[/]");
            return 1;
        }

        if (assetPath != "SKILL.md")
        {
            if (!File.Exists(fullPath))
            {
                if (CreateSkillFile(fullPath, content, out var createError))
                {
                    AnsiConsole.MarkupLineInterpolated($"[green]✓[/] {Markup.Escape(relativePath)} [green]created[/]");
                    return 0;
                }

                AnsiConsole.MarkupLineInterpolated($"[red]✗[/] {Markup.Escape(relativePath)} [red]error: {Markup.Escape(createError!)}[/]");
                return 1;
            }

            string existingReference;
            try
            {
                existingReference = File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"[red]✗[/] {Markup.Escape(relativePath)} [red]error: {Markup.Escape(ex.Message)}[/]");
                return 1;
            }

            if (!existingReference.Contains(ManagedSkillReferenceMarker, StringComparison.Ordinal))
            {
                AnsiConsole.MarkupLineInterpolated($"[yellow]•[/] {Markup.Escape(relativePath)} [yellow]preserved (not a managed t2i reference)[/]");
                return 0;
            }

            if (string.Equals(existingReference, content, StringComparison.Ordinal))
            {
                AnsiConsole.MarkupLineInterpolated($"[dim]•[/] {Markup.Escape(relativePath)} [dim]current[/]");
                return 0;
            }
        }
        else if (string.Equals(existingMainContent, content, StringComparison.Ordinal))
        {
            AnsiConsole.MarkupLineInterpolated($"[dim]•[/] {Markup.Escape(relativePath)} [dim]current[/]");
            return 0;
        }

        if (ReplaceSkillFile(fullPath, content, out var error))
        {
            AnsiConsole.MarkupLineInterpolated($"[green]✓[/] {Markup.Escape(relativePath)} [green]updated[/]");
            return 0;
        }

        AnsiConsole.MarkupLineInterpolated($"[red]✗[/] {Markup.Escape(relativePath)} [red]error: {Markup.Escape(error!)}[/]");
        return 1;
    }

    private static bool IsWithinDirectory(string directory, string path)
    {
        var normalizedDirectory = Path.GetFullPath(directory);
        var directoryPrefix = Path.EndsInDirectorySeparator(normalizedDirectory)
            ? normalizedDirectory
            : normalizedDirectory + Path.DirectorySeparatorChar;
        return path.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CreateSkillFile(string fullPath, string content, out string? error)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(content);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
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
