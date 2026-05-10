#if NET10_0_OR_GREATER
using System.Diagnostics;
using System.IO.Compression;
using Xunit;

namespace ElBruno.Text2Image.Tests.Cli;

public class ToolPackagingGuardrailTests
{
    [Fact]
    public async Task CliPackage_ContainsDotnetToolSettings_AndInstallsAsTool()
    {
        var projectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ElBruno.Text2Image.Cli", "ElBruno.Text2Image.Cli.csproj"));

        var outputDir = Path.Combine(Path.GetTempPath(), $"t2i-pack-guardrail-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDir);

        try
        {
            var version = "9.9.9-guardrail";
            await RunProcessAsync(
                "dotnet",
                $"pack \"{projectPath}\" -c Release -o \"{outputDir}\" -p:Version={version}",
                outputDir);

            var packagePath = Directory
                .GetFiles(outputDir, "ElBruno.Text2Image.Cli.*.nupkg", SearchOption.TopDirectoryOnly)
                .Single(path => !path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase));

            using (var archive = ZipFile.OpenRead(packagePath))
            {
                Assert.Contains(
                    archive.Entries,
                    e => string.Equals(e.FullName, "tools/net8.0/any/DotnetToolSettings.xml", StringComparison.Ordinal));
                Assert.Contains(
                    archive.Entries,
                    e => string.Equals(e.FullName, "tools/net10.0/any/DotnetToolSettings.xml", StringComparison.Ordinal));
            }

            var installDir = Path.Combine(outputDir, "tool-install");
            Directory.CreateDirectory(installDir);

            await RunProcessAsync("dotnet", "new tool-manifest --force", installDir);
            await RunProcessAsync(
                "dotnet",
                $"tool install ElBruno.Text2Image.Cli --version {version} --add-source \"{outputDir}\" --ignore-failed-sources",
                installDir);
            await RunProcessAsync("dotnet", "tool run t2i -- --help", installDir);
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, recursive: true);
            }
        }
    }

    private static async Task RunProcessAsync(string fileName, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        Assert.NotNull(process);

        var stdOut = await process.StandardOutput.ReadToEndAsync();
        var stdErr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.True(
            process.ExitCode == 0,
            $"Command failed: {fileName} {arguments}{Environment.NewLine}STDOUT:{Environment.NewLine}{stdOut}{Environment.NewLine}STDERR:{Environment.NewLine}{stdErr}");
    }
}
#endif
