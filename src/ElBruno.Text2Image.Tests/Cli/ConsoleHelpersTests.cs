#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Tui;

namespace ElBruno.Text2Image.Tests.Cli;

public class ConsoleHelpersTests
{
    [Theory]
    [InlineData("Hello World!", "hello-world")]
    [InlineData("A cat in a hat", "a-cat-in-a-hat")]
    [InlineData("Test!@#$%^&*()", "test")]
    [InlineData("___underscores___", "underscores")]
    public void Slug_StripsSpecialChars_LowercasesAndJoins(string input, string expected)
    {
        var result = ConsoleHelpers.Slug(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Slug_CollapsesMultipleSpaces()
    {
        var result = ConsoleHelpers.Slug("Multiple   spaces");
        Assert.Contains("multiple", result);
        Assert.Contains("spaces", result);
    }

    [Fact]
    public void Slug_HandlesUnicode()
    {
        var result = ConsoleHelpers.Slug("Ünîcödé Tëxt");
        Assert.NotEmpty(result);
        Assert.DoesNotContain("Ü", result);
        Assert.DoesNotContain("Ë", result);
    }

    [Fact]
    public void Slug_TruncatesToMaxLength()
    {
        var longText = new string('a', 100);
        var result = ConsoleHelpers.Slug(longText, max: 20);

        Assert.Equal(20, result.Length);
        Assert.All(result, c => Assert.Equal('a', c));
    }

    [Theory]
    [InlineData("", "output")]
    [InlineData("   ", "output")]
    [InlineData(null, "output")]
    [InlineData("!@#$%^&*()", "output")]
    public void Slug_ReturnsDefault_ForInvalidInput(string? input, string expected)
    {
        var result = ConsoleHelpers.Slug(input!);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Slug_TrimsTrailingDashes()
    {
        var result = ConsoleHelpers.Slug("test---", max: 10);
        Assert.Equal("test", result);
    }

    [Theory]
    [InlineData("sk-1234567890abcdef", "sk-123***...***cdef")]
    [InlineData("api-key-super-secret-long", "api-ke***...***long")]
    [InlineData("1234567890", "123456***...***7890")]
    public void Mask_RevealsFirstAndLast4_HidesMiddle(string secret, string expected)
    {
        var result = ConsoleHelpers.Mask(secret);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("short", "*****")]
    [InlineData("abc", "***")]
    [InlineData("12345678", "********")]
    public void Mask_HandlesShortSecrets_GracefullyWithoutInfoLeak(string secret, string expected)
    {
        var result = ConsoleHelpers.Mask(secret);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "****")]
    [InlineData("   ", "****")]
    [InlineData(null, "****")]
    public void Mask_ReturnsPlaceholder_ForEmptyOrNull(string? secret, string expected)
    {
        var result = ConsoleHelpers.Mask(secret!);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsInteractive_ReturnsBooleanValue()
    {
        var result = ConsoleHelpers.IsInteractive();
        Assert.IsType<bool>(result);
    }
}
#endif
