#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Commands;
using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Tests;

/// <summary>
/// Tests for InitCommand — writes embedded SKILL.md to .github/skills/t2i/ and .claude/skills/t2i/
/// </summary>
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
        var settings = new InitCommand.Settings { Target = "all", Force = false };
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
        var settings = new InitCommand.Settings { Target = "github", Force = false };
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
        var settings = new InitCommand.Settings { Target = "claude", Force = false };
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
    public void Init_SkipsExistingFile_WithoutForce()
    {
        const string sentinel = "PRE-EXISTING CONTENT";
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        
        // Pre-create file with sentinel content
        Directory.CreateDirectory(Path.GetDirectoryName(githubPath)!);
        File.WriteAllText(githubPath, sentinel);
        
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "github", Force = false };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(0, exitCode);
        
        var content = File.ReadAllText(githubPath);
        Assert.Equal(sentinel, content);
        Assert.DoesNotContain("# t2i", content);
    }

    [Fact]
    public void Init_OverwritesExistingFile_WithForce()
    {
        const string sentinel = "PRE-EXISTING CONTENT";
        var githubPath = Path.Combine(_testDir, ".github", "skills", "t2i", "SKILL.md");
        
        // Pre-create file with sentinel content
        Directory.CreateDirectory(Path.GetDirectoryName(githubPath)!);
        File.WriteAllText(githubPath, sentinel);
        
        var command = new InitCommand();
        var settings = new InitCommand.Settings { Target = "github", Force = true };
        var context = CreateContext();

        var exitCode = command.Execute(context, settings);

        Assert.Equal(0, exitCode);
        
        var content = File.ReadAllText(githubPath);
        Assert.DoesNotContain(sentinel, content);
        Assert.Contains("# t2i", content);
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
        var settings = new InitCommand.Settings { Target = "all", Force = false };
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
