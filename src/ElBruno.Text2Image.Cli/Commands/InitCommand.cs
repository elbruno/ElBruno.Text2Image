using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Initializes the current folder with a t2i skill file for AI coding agents.
/// </summary>
internal sealed class InitCommand : Command<InitCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--target <TARGET>")]
        [Description("Target platform(s): github, claude, or all (default: all)")]
        [DefaultValue("all")]
        public string Target { get; set; } = "all";

        [CommandOption("--keep-existing")]
        [Description("Skip files that already exist (default: update existing files)")]
        public bool KeepExisting { get; set; }
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
        var results = new List<(string Path, string Status)>();

        foreach (var target in targets)
        {
            var relativeDirectory = GetRelativeDirectory(target);
            var fullDirectory = Path.GetFullPath(Path.Combine(cwd, relativeDirectory));

            foreach (var (assetPath, content) in skillFiles)
            {
                var fullPath = Path.GetFullPath(Path.Combine(fullDirectory, assetPath));
                if (!IsWithinDirectory(fullDirectory, fullPath))
                {
                    results.Add((Path.Combine(relativeDirectory, assetPath), "error: invalid skill path"));
                    continue;
                }

                var relativePath = Path.Combine(relativeDirectory, assetPath);
                var (status, _) = WriteSkillFile(fullPath, content, settings.KeepExisting);
                results.Add((relativePath, status));
            }
        }

        // Display results
        AnsiConsole.WriteLine();
        foreach (var (path, status) in results)
        {
            var escapedPath = Markup.Escape(path);
            var (icon, color) = status switch
            {
                "created" => ("✓", "green"),
                "updated" => ("→", "yellow"),
                "skipped" => ("•", "dim"),
                _ => ("✗", "red")
            };

            AnsiConsole.MarkupLineInterpolated($"[{color}]{icon}[/] {escapedPath} [{color}]{status}[/]");
        }

        // Summary panel
        var createdCount = results.Count(r => r.Status == "created");
        var updatedCount = results.Count(r => r.Status == "updated");
        var skippedCount = results.Count(r => r.Status == "skipped");

        var summaryLines = new List<string>();
        if (createdCount > 0)
            summaryLines.Add($"[green]{createdCount} created[/]");
        if (updatedCount > 0)
            summaryLines.Add($"[yellow]{updatedCount} updated[/]");
        if (skippedCount > 0)
            summaryLines.Add($"[dim]{skippedCount} skipped (use --keep-existing to preserve)[/]");

        if (summaryLines.Count > 0)
        {
            AnsiConsole.WriteLine();
            var panel = new Panel(string.Join(", ", summaryLines))
            {
                Header = new PanelHeader("Summary", Justify.Left),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Grey)
            };
            AnsiConsole.Write(panel);
        }

        return results.Any(result => result.Status.StartsWith("error", StringComparison.Ordinal)) ? 1 : 0;
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

    private static bool IsWithinDirectory(string directory, string path)
    {
        var normalizedDirectory = Path.GetFullPath(directory);
        var directoryPrefix = Path.EndsInDirectorySeparator(normalizedDirectory)
            ? normalizedDirectory
            : normalizedDirectory + Path.DirectorySeparatorChar;
        return path.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static (string Status, bool Written) WriteSkillFile(string fullPath, string content, bool keepExisting)
    {
        var exists = File.Exists(fullPath);

        if (exists && keepExisting)
        {
            return ("skipped", false);
        }

        try
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, content);
            return (exists ? "updated" : "created", true);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error writing {Markup.Escape(fullPath)}:[/] {Markup.Escape(ex.Message)}");
            return ("error", false);
        }
    }
}
