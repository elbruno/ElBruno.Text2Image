#if NET10_0_OR_GREATER
using Xunit;
using System.Diagnostics;

namespace ElBruno.Text2Image.Tests.Cli;

[Trait("Category", "Smoke")]
public class CommandSurfaceSmokeTest
{
    [Fact(Skip = "Requires CLI implementation to be complete - smoke test for command wiring")]
    public async Task CliHelp_ExposesExpectedCommands()
    {
        var projectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ElBruno.Text2Image.Cli"));

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --no-build -- --help",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        Assert.NotNull(process);

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var combinedOutput = output + error;

        Assert.Contains("generate", combinedOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("config", combinedOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secrets", combinedOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("doctor", combinedOutput, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("providers", combinedOutput, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(Skip = "Requires CLI implementation to be complete - smoke test for version command")]
    public async Task VersionCommand_Runs_WithoutError()
    {
        var projectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ElBruno.Text2Image.Cli"));

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --no-build -- --version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        Assert.NotNull(process);

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.Equal(0, process.ExitCode);
        Assert.NotEmpty(output.Trim());
    }
}
#endif
