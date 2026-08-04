using ElBruno.Text2Image.BlazorComponents.Models;
using ElBruno.Text2Image;

namespace ElBruno.Text2Image.BlazorComponents.Services;

public sealed class Text2ImageState
{
    public event Action? Changed;
    public InferenceProgress Progress { get; private set; } = new(null, null);
    public ExecutionProvider Backend { get; set; } = ExecutionProvider.Auto;
    public IReadOnlyList<ImageGenerationResult> Images { get; private set; } = Array.Empty<ImageGenerationResult>();

    public void SetProgress(InferenceProgress progress) { Progress = progress; Changed?.Invoke(); }
    public void SetImages(IReadOnlyList<ImageGenerationResult> images) { Images = images; Changed?.Invoke(); }
}
