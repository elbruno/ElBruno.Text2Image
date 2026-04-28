#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Commands;
using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Tests;

/// <summary>
/// Tests for InitCommand — writes embedded SKILL.md to .github/skills/t2i/ and .claude/skills/t2i/
/// Serialized execution: modifies global Directory.CurrentDirectory, must not run in parallel.
/// </summary>
[Collection("Global State")]
public class InitCommandTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _originalCwd;

    public InitCommandTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"t2i-init-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _originalCwd = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_testDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalCwd);
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public void Init_WritesBothFiles_WhenTargetAll()
    {
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "all", KeepExisting = false };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(0, exitCode);
        
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        var claudePath = Path.Combine(_testDir, ".claude", "skills", "t2i", "SKILL.md");
        
        Assert.True(File.Exists(githubPath), $"Expected {githubPath} to exist");
        Assert.True(File.Exists(claudePath), $"Expected {claudePath} to exist");
        
        var githubContent = File.ReadAllText(githubPath);
        var claudeContent = File.ReadAllText(claudePath);
        
        Assert.Contains("# t2i", githubContent);
        Assert.NotEmpty(githubContent);
        Assert.Contains("# t2i", claudeContent);
        Assert.NotEmpty(claudeContent);
    }

    [Fact]
    public void Init_WritesOnlyGithub_WhenTargetGithub()
    {
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "github", KeepExisting = false };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(0, exitCode);
        
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        var claudePath = Path.Combine(_testDir, ".claude", "skills", "t2i", "SKILL.md");
        
        Assert.True(File.Exists(githubPath), $"Expected {githubPath} to exist");
        Assert.False(File.Exists(claudePath), $"Expected {claudePath} NOT to exist");
        
        var githubContent = File.ReadAllText(githubPath);
        Assert.Contains("# t2i", githubContent);
    }

    [Fact]
    public void Init_WritesOnlyClaude_WhenTargetClaude()
    {
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "claude", KeepExisting = false };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(0, exitCode);
        
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        var claudePath = Path.Combine(_testDir, ".claude", "skills", "t2i", "SKILL.md");
        
        Assert.False(File.Exists(githubPath), $"Expected {githubPath} NOT to exist");
        Assert.True(File.Exists(claudePath), $"Expected {claudePath} to exist");
        
        var claudeContent = File.ReadAllText(claudePath);
        Assert.Contains("# t2i", claudeContent);
    }

    [Fact]
    public void Init_UpdatesExistingFile_ByDefault()
    {
        const string sentinel = "OLD CONTENT";
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        
        // Pre-create file with sentinel content
        Directory.CreateDirectory(Path.GetDirectoryName(githubPath)!);
        File.WriteAllText(githubPath, sentinel);
        
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "github", KeepExisting = false };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(0, exitCode);
        
        var content = File.ReadAllText(githubPath);
        Assert.DoesNotContain(sentinel, content);
        Assert.Contains("# t2i", content);
    }

    [Fact]
    public void Init_KeepsExistingFile_WithKeepExisting()
    {
        const string sentinel = "KEEP THIS CONTENT";
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        
        // Pre-create file with sentinel content
        Directory.CreateDirectory(Path.GetDirectoryName(githubPath)!);
        File.WriteAllText(githubPath, sentinel);
        
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "github", KeepExisting = true };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(0, exitCode);
        
        var content = File.ReadAllText(githubPath);
        Assert.Equal(sentinel, content);
        Assert.DoesNotContain("# t2i", content);
    }

    [Fact]
    public void Init_CreatesParentDirectories()
    {
        // Ensure directories don't exist
        var githubDir = Path.Combine(_testDir, ".github");
        var claudeDir = Path.Combine(_testDir, ".claude");
        
        Assert.False(Directory.Exists(githubDir));
        Assert.False(Directory.Exists(claudeDir));
        
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "all", KeepExisting = false };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(0, exitCode);
        
        Assert.True(Directory.Exists(Path.Combine(githubDir, "skills", "t2i")));
        Assert.True(Directory.Exists(Path.Combine(claudeDir, "skills", "t2i")));
        
        var githubPath = Path.Combine(githubDir, "skills", "t2i", "SKILL.md");
        var claudePath = Path.Combine(claudeDir, "skills", "t2i", "SKILL.md");
        
        Assert.True(File.Exists(githubPath));
        Assert.True(File.Exists(claudePath));
    }

    [Fact]
    public void Init_ReturnsError_WhenInvalidTarget()
    {
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "invalid-target", KeepExisting = false };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(1, exitCode);
        
        // Files should not be created
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        var claudePath = Path.Combine(_testDir, ".claude", "skills", "t2i", "SKILL.md");
        
        Assert.False(File.Exists(githubPath));
        Assert.False(File.Exists(claudePath));
    }

    [Fact]
    public void Init_IsIdempotent_MultipleCallsSameResult()
    {
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "all", KeepExisting = false };
        var context = CreateContext();

        // First call - creates files
        var exitCode1 = command.Execute(context, settings);
        Assert.Equal(0, exitCode1);
        
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        var claudePath = Path.Combine(_testDir, ".claude", "skills", "t2i", "SKILL.md");
        
        var firstGithubContent = File.ReadAllText(githubPath);
        var firstClaudeContent = File.ReadAllText(claudePath);
        
        // Second call - updates existing files to same content
        var exitCode2 = command.Execute(context, settings);
        Assert.Equal(0, exitCode2);
        
        var secondGithubContent = File.ReadAllText(githubPath);
        var secondClaudeContent = File.ReadAllText(claudePath);
        
        // Content should be unchanged (idempotent)
        Assert.Equal(firstGithubContent, secondGithubContent);
        Assert.Equal(firstClaudeContent, secondClaudeContent);
    }

    [Fact]
    public void Init_WithKeepExisting_SkipsAllExistingFiles()
    {
        const string sentinel = "OLD CONTENT";
        
        // Pre-create both files with sentinel content
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        var claudePath = Path.Combine(_testDir, ".claude", "skills", "t2i", "SKILL.md");
        
        Directory.CreateDirectory(Path.GetDirectoryName(githubPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(claudePath)!);
        
        File.WriteAllText(githubPath, sentinel);
        File.WriteAllText(claudePath, sentinel);
        
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "all", KeepExisting = true };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(0, exitCode);
        
        var githubContent = File.ReadAllText(githubPath);
        var claudeContent = File.ReadAllText(claudePath);
        
        // Both should be kept (not overwritten)
        Assert.Equal(sentinel, githubContent);
        Assert.Equal(sentinel, claudeContent);
        Assert.DoesNotContain("# t2i", githubContent);
        Assert.DoesNotContain("# t2i", claudeContent);
    }

    [Fact]
    public void Init_DefaultTargetIsAll()
    {
        var settings = new InitCommand.Settings();
        Assert.Equal("all", settings.Target);
    }

    [Fact]
    public void Init_DefaultKeepExistingIsFalse()
    {
        var settings = new InitCommand.Settings();
        Assert.False(settings.KeepExisting);
    }

    [Theory]
    [InlineData("github")]
    [InlineData("claude")]
    [InlineData("all")]
    [InlineData("GITHUB")]
    [InlineData("CLAUDE")]
    [InlineData("ALL")]
    [InlineData("Github")]
    [InlineData("Claude")]
    [InlineData("All")]
    public void Init_AcceptsCaseInsensitiveTargets(string target)
    {
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = target, KeepExisting = false };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        // Should succeed with any valid case variation
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Init_FilesContainExpectedContent()
    {
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "all", KeepExisting = false };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(0, exitCode);
        
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        var claudePath = Path.Combine(_testDir, ".claude", "skills", "t2i", "SKILL.md");
        
        var githubContent = File.ReadAllText(githubPath);
        var claudeContent = File.ReadAllText(claudePath);
        
        // Verify content is identical (both come from same embedded resource)
        Assert.Equal(githubContent, claudeContent);
        
        // Verify content contains expected markers from SKILL.md
        Assert.Contains("# t2i", githubContent);
        Assert.Contains("text-to-image", githubContent, StringComparison.OrdinalIgnoreCase);
    }

    private static CommandContext CreateContext()
    {
        var remaining = new FakeRemainingArguments();
        return new CommandContext(
            Array.Empty<string>(),
            remaining,
            "init",
            null
        );
    }

    private sealed class FakeRemainingArguments : IRemainingArguments
    {
        public IReadOnlyList<string> Raw => Array.Empty<string>();
        public ILookup<string, string?> Parsed => Enumerable.Empty<string>().ToLookup(x => x, x => (string?)null);
    }
}
#endif
