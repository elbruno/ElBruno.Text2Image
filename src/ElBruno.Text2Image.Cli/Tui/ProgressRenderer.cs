using Spectre.Console;
using ElBruno.Text2Image.Cli.Providers;

namespace ElBruno.Text2Image.Cli.Tui;

/// <summary>
/// Renders progress updates during image generation.
/// </summary>
internal sealed class ProgressRenderer : IProgress<GenerationProgress>
{
    // TODO(Kaylee): implement Spectre.Console progress bar rendering
    public void Report(GenerationProgress value)
    {
        throw new NotImplementedException();
    }
}
