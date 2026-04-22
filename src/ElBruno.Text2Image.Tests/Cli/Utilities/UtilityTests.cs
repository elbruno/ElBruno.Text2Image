#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Tui;

namespace ElBruno.Text2Image.Tests.Cli.Utilities;

/// <summary>
/// Phase 3B: Utility layer tests for string helpers, encoding, error formatting.
/// Tests ConsoleHelpers masking, slugification, and message formatting.
/// </summary>
public class UtilityTests
{
    #region ConsoleHelpers.Mask Tests

    [Fact]
    public void ConsoleHelpers_Mask_MasksLongSecret()
    {
        var secret = "sk-1234567890abcdefghijklmnopqrstuvwxyz";
        var masked = ConsoleHelpers.Mask(secret);

        Assert.DoesNotContain("1234567890", masked);
        Assert.StartsWith("sk-123", masked);
        Assert.Contains("***...***", masked);
    }

    [Fact]
    public void ConsoleHelpers_Mask_MasksShortSecret()
    {
        var secret = "abc123";
        var masked = ConsoleHelpers.Mask(secret);

        Assert.Equal("******", masked);
    }

    [Fact]
    public void ConsoleHelpers_Mask_HandlesNullString()
    {
        var masked = ConsoleHelpers.Mask(null!);

        Assert.Equal("****", masked);
    }

    [Fact]
    public void ConsoleHelpers_Mask_HandlesEmptyString()
    {
        var masked = ConsoleHelpers.Mask("");

        Assert.Equal("****", masked);
    }

    [Fact]
    public void ConsoleHelpers_Mask_HandlesWhitespace()
    {
        var masked = ConsoleHelpers.Mask("   ");

        Assert.Equal("****", masked);
    }

    #endregion

    #region ConsoleHelpers.Slug Tests

    [Fact]
    public void ConsoleHelpers_Slug_ConvertsToLowercase()
    {
        var slug = ConsoleHelpers.Slug("HELLO WORLD");

        Assert.Equal("hello-world", slug);
    }

    [Fact]
    public void ConsoleHelpers_Slug_ReplacesSpacesWithDashes()
    {
        var slug = ConsoleHelpers.Slug("my test prompt");

        Assert.Equal("my-test-prompt", slug);
    }

    [Fact]
    public void ConsoleHelpers_Slug_RemovesSpecialCharacters()
    {
        var slug = ConsoleHelpers.Slug("hello@world#test!");

        Assert.Equal("helloworldtest", slug);
    }

    [Fact]
    public void ConsoleHelpers_Slug_TruncatesToMaxLength()
    {
        var longText = new string('a', 100);
        var slug = ConsoleHelpers.Slug(longText, max: 20);

        Assert.Equal(20, slug.Length);
    }

    [Fact]
    public void ConsoleHelpers_Slug_HandlesNullString()
    {
        var slug = ConsoleHelpers.Slug(null!);

        Assert.Equal("output", slug);
    }

    [Fact]
    public void ConsoleHelpers_Slug_HandlesEmptyString()
    {
        var slug = ConsoleHelpers.Slug("");

        Assert.Equal("output", slug);
    }

    [Fact]
    public void ConsoleHelpers_Slug_HandlesWhitespace()
    {
        var slug = ConsoleHelpers.Slug("   ");

        Assert.Equal("output", slug);
    }

    [Fact]
    public void ConsoleHelpers_Slug_PreservesAlphanumeric()
    {
        var slug = ConsoleHelpers.Slug("test123abc");

        Assert.Equal("test123abc", slug);
    }

    [Fact]
    public void ConsoleHelpers_Slug_TrimsDashes()
    {
        var slug = ConsoleHelpers.Slug("---hello---world---");

        Assert.Equal("hello---world", slug);
    }

    [Fact]
    public void ConsoleHelpers_Slug_HandlesOnlySpecialCharacters()
    {
        var slug = ConsoleHelpers.Slug("!@#$%^&*()");

        Assert.Equal("output", slug);
    }

    #endregion
}
#endif
