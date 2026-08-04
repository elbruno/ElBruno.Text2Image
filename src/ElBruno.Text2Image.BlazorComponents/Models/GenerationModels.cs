namespace ElBruno.Text2Image.BlazorComponents.Models;

using ElBruno.Text2Image;

/// <summary>Values entered by <see cref="Components.PromptEditor"/>.</summary>
public sealed record GenerationRequest(string Prompt, ImageGenerationOptions Options, string? NegativePrompt);

/// <summary>Optional state supplied by an application that can report inference progress.</summary>
public sealed record InferenceProgress(int? CurrentStep, int? TotalSteps, TimeSpan? EstimatedRemaining = null);
