using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Checks for and installs updates to the t2i global tool.
/// </summary>
internal sealed class UpdateCommand : Command<UpdateCommand.Settings>
{
    private const string ToolPackageName = "ElBruno.Text2Image.Cli";

    public sealed class Settings : CommandSettings
    {
        [CommandOption("--auto")]
        [Description("Automatically update without confirmation if a newer version is available")]
        public bool Auto { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var currentVersion = GetCurrentVersion();
            AnsiConsole.MarkupLineInterpolated($"[dim]Current version:[/] {Markup.Escape(currentVersion)}");

            var (availableVersion, isUpdateAvailable) = CheckForUpdates();
            if (!isUpdateAvailable)
            {
                AnsiConsole.MarkupLine("[green]✓[/] You are running the latest version.");
                return 0;
            }

            AnsiConsole.MarkupLineInterpolated($"[yellow]New version available:[/] {Markup.Escape(availableVersion)}");
            AnsiConsole.WriteLine();

            if (settings.Auto)
            {
                return PerformUpdate();
            }

            // Prompt user for confirmation
            if (!AnsiConsole.Confirm("[yellow]Update now?[/]", false))
            {
                AnsiConsole.MarkupLine("[dim]Update cancelled.[/]");
                return 0;
            }

            return PerformUpdate();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static string GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        
        // Try to get informational version (includes build metadata if available)
        var infoVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? version;

        return infoVersion;
    }

    private static (string Version, bool IsUpdateAvailable) CheckForUpdates()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"tool update {ToolPackageName} --check-only -g",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new Exception("Failed to start dotnet process");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            // Check for "A newer version" in the output
            if (output.Contains("A newer version", StringComparison.OrdinalIgnoreCase))
            {
                // Extract version from output like "A newer version of 'ElBruno.Text2Image.Cli' is available: 1.2.3"
                var match = Regex.Match(output, @"available:\s*([0-9\.]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return (match.Groups[1].Value, true);
                }

                return ("latest", true);
            }

            // No update available
            return ("", false);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[dim]Warning:[/] Could not check for updates: {Markup.Escape(ex.Message)}");
            return ("", false);
        }
    }

    private static int PerformUpdate()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"tool update {ToolPackageName} -g",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            AnsiConsole.MarkupLine("[dim]Updating t2i...[/]");
            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new Exception("Failed to start dotnet process");
            }

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                AnsiConsole.MarkupLine("[green]✓[/] Update completed successfully.");
                var newVersion = GetCurrentVersion();
                AnsiConsole.MarkupLineInterpolated($"[dim]New version:[/] {Markup.Escape(newVersion)}");
                return 0;
            }

            AnsiConsole.MarkupLine("[red]✗[/] Update failed. Please check your dotnet installation or run: dotnet tool update ElBruno.Text2Image.Cli -g");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}
