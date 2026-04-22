#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Providers;

namespace ElBruno.Text2Image.Tests.Cli.TUI;

/// <summary>
/// Phase 3B: TUI component tests for progress reporting and generation progress.
/// Tests progress data structures and validation.
/// </summary>
public class ComponentTests
{
    #region GenerationProgress Tests

    [Fact]
    public void GenerationProgress_Constructor_SetsProperties()
    {
        var progress = new GenerationProgress(3, 5, "Test message");

        Assert.Equal(3, progress.Step);
        Assert.Equal(5, progress.TotalSteps);
        Assert.Equal("Test message", progress.Message);
    }

    [Fact]
    public void GenerationProgress_Constructor_AllowsNullMessage()
    {
        var progress = new GenerationProgress(1, 1, null);

        Assert.Equal(1, progress.Step);
        Assert.Equal(1, progress.TotalSteps);
        Assert.Null(progress.Message);
    }

    [Fact]
    public void GenerationProgress_Constructor_AllowsEmptyMessage()
    {
        var progress = new GenerationProgress(2, 4, "");

        Assert.Equal(2, progress.Step);
        Assert.Equal(4, progress.TotalSteps);
        Assert.Equal("", progress.Message);
    }

    [Fact]
    public void GenerationProgress_Constructor_HandlesNegativeStep()
    {
        var progress = new GenerationProgress(-1, 10, "Negative");

        Assert.Equal(-1, progress.Step);
        Assert.Equal(10, progress.TotalSteps);
    }

    [Fact]
    public void GenerationProgress_Constructor_HandlesZeroSteps()
    {
        var progress = new GenerationProgress(0, 0, "Starting...");

        Assert.Equal(0, progress.Step);
        Assert.Equal(0, progress.TotalSteps);
    }

    [Fact]
    public void GenerationProgress_Constructor_HandlesLargeSteps()
    {
        var progress = new GenerationProgress(500, 1000, "Processing...");

        Assert.Equal(500, progress.Step);
        Assert.Equal(1000, progress.TotalSteps);
    }

    [Fact]
    public void GenerationProgress_Constructor_HandlesWhitespaceMessage()
    {
        var progress = new GenerationProgress(2, 10, "   ");

        Assert.Equal(2, progress.Step);
        Assert.Equal(10, progress.TotalSteps);
        Assert.Equal("   ", progress.Message);
    }

    [Fact]
    public void GenerationProgress_Constructor_HandlesLongMessage()
    {
        var longMessage = new string('a', 500);
        var progress = new GenerationProgress(1, 2, longMessage);

        Assert.Equal(1, progress.Step);
        Assert.Equal(2, progress.TotalSteps);
        Assert.Equal(longMessage, progress.Message);
    }

    [Fact]
    public void GenerationProgress_Constructor_HandlesMaxInt()
    {
        var progress = new GenerationProgress(int.MaxValue, int.MaxValue, "Max");

        Assert.Equal(int.MaxValue, progress.Step);
        Assert.Equal(int.MaxValue, progress.TotalSteps);
    }

    [Fact]
    public void GenerationProgress_Constructor_HandlesStepGreaterThanTotal()
    {
        var progress = new GenerationProgress(10, 5, "Over");

        Assert.Equal(10, progress.Step);
        Assert.Equal(5, progress.TotalSteps);
    }

    #endregion
}
#endif
