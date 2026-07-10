#if NET10_0_OR_GREATER
using System.Diagnostics;
using ElBruno.Text2Image.Cli.Commands;
using Xunit;

namespace ElBruno.Text2Image.Tests.Cli;

/// <summary>
/// End-to-end command tests for installing and refreshing the agent skill.
/// </summary>
[Collection("Global State")]
public sealed class SkillMaintenanceCommandTests : IDisposable
{
    private readonly string _workspace;

    public SkillMaintenanceCommandTests()
    {
        _workspace = Path.Combine(
            AppContext.BaseDirectory,
            $"skill-maintenance-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspace);
    }

    [Fact]
    public async Task Init_GeneratesSkillsWithCurrentModelsAndUpgradeGuidance()
    {
        var result = await RunCliAsync("init");

        Assert.Equal(0, result.ExitCode);

        foreach (var path in SkillPaths)
        {
            Assert.True(File.Exists(path), $"Expected skill file '{path}' to be created.");

            var content = await File.ReadAllTextAsync(path);
            Assert.Contains("foundry-flux2", content);
            Assert.Contains("FLUX.2 Pro", content);
            Assert.Contains("foundry-mai2", content);
            Assert.Contains("MAI-Image-2", content);
            Assert.Contains("foundry-mai25", content);
            Assert.Contains("MAI-Image-2.5", content);
            Assert.Contains("foundry-mai25-flash", content);
            Assert.Contains("MAI-Image-2.5-Flash", content);
            Assert.Contains("foundry-gpt-image-1p5", content);
            Assert.Contains("GPT-Image-1.5", content);
            Assert.Contains("foundry-gpt-image-2", content);
            Assert.Contains("GPT-Image-2", content);
            Assert.Contains("t2i upgrade", content);
        }
    }

    [Fact]
    public async Task Upgrade_IsListedInRootHelp()
    {
        var result = await RunCliAsync("--help");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("upgrade", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upgrade_UpdatesAnExistingSkillWithoutCreatingMissingTargets()
    {
        var githubPath = SkillPaths[0];
        var unrelatedSkillPath = Path.Combine(_workspace, ".github", "skills", "other", "SKILL.md");
        Directory.CreateDirectory(Path.GetDirectoryName(githubPath)!);
        await File.WriteAllTextAsync(githubPath, "<!-- t2i:managed-skill -->\noutdated skill content");
        Directory.CreateDirectory(Path.GetDirectoryName(unrelatedSkillPath)!);
        await File.WriteAllTextAsync(unrelatedSkillPath, "unrelated skill content");

        var result = await RunCliAsync("upgrade");

        Assert.Equal(0, result.ExitCode);
        var content = await File.ReadAllTextAsync(githubPath);
        Assert.DoesNotContain("outdated skill content", content);
        Assert.Contains("MAI-Image-2.5-Flash", content);
        Assert.False(File.Exists(SkillPaths[1]));
        Assert.Equal("unrelated skill content", await File.ReadAllTextAsync(unrelatedSkillPath));
    }

    [Fact]
    public async Task Upgrade_IsIdempotent()
    {
        var initResult = await RunCliAsync("init");
        Assert.Equal(0, initResult.ExitCode);

        var firstUpgrade = await RunCliAsync("upgrade");
        Assert.Equal(0, firstUpgrade.ExitCode);
        var firstContents = await ReadAllSkillsAsync();

        var secondUpgrade = await RunCliAsync("upgrade");
        Assert.Equal(0, secondUpgrade.ExitCode);
        var secondContents = await ReadAllSkillsAsync();

        Assert.Equal(firstContents, secondContents);
    }

    [Fact]
    public async Task Upgrade_SucceedsWithoutInstalledSkillsAndDoesNotCreateAny()
    {
        var result = await RunCliAsync("upgrade");

        Assert.Equal(0, result.ExitCode);
        Assert.All(SkillPaths, path => Assert.False(File.Exists(path)));
        Assert.False(Directory.Exists(Path.Combine(_workspace, ".github")));
        Assert.False(Directory.Exists(Path.Combine(_workspace, ".claude")));
    }

    [Fact]
    public async Task Upgrade_RejectsAnInvalidTargetWithoutChangingSkills()
    {
        var initResult = await RunCliAsync("init");
        Assert.Equal(0, initResult.ExitCode);
        var before = await ReadAllSkillsAsync();

        var result = await RunCliAsync("upgrade", "--target", "unsupported");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(before, await ReadAllSkillsAsync());
    }

    [Fact]
    public async Task Upgrade_ReturnsFailureAndPreservesContentWhenAnExistingSkillIsLocked()
    {
        var githubPath = SkillPaths[0];
        const string originalContent = "locked skill content";
        Directory.CreateDirectory(Path.GetDirectoryName(githubPath)!);
        await File.WriteAllTextAsync(githubPath, originalContent);

        (int ExitCode, string Output) result;
        using (var lockStream = new FileStream(githubPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            result = await RunCliAsync("upgrade", "--target", "github");
        }

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(originalContent, await File.ReadAllTextAsync(githubPath));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(githubPath)!, "*.tmp"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspace))
        {
            Directory.Delete(_workspace, recursive: true);
        }
    }

    private string[] SkillPaths =>
    [
        Path.Combine(_workspace, ".github", "skills", "t2i", "SKILL.md"),
        Path.Combine(_workspace, ".claude", "skills", "t2i", "SKILL.md")
    ];

    private async Task<string[]> ReadAllSkillsAsync()
    {
        var contents = new string[SkillPaths.Length];
        for (var i = 0; i < SkillPaths.Length; i++)
        {
            contents[i] = await File.ReadAllTextAsync(SkillPaths[i]);
        }

        return contents;
    }

    private async Task<(int ExitCode, string Output)> RunCliAsync(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = _workspace,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add(typeof(InitCommand).Assembly.Location);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, await standardOutput + await standardError);
    }
}
#endif
