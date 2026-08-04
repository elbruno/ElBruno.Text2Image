using Bunit;
using Microsoft.AspNetCore.Components;
using ElBruno.Text2Image;
using ElBruno.Text2Image.BlazorComponents.Components;
using ElBruno.Text2Image.BlazorComponents.Extensions;
using ElBruno.Text2Image.BlazorComponents.Models;
using ElBruno.Text2Image.BlazorComponents.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ElBruno.Text2Image.BlazorComponents.Tests;

public sealed class BlazorComponentTests : TestContext
{
    public BlazorComponentTests() => Services.AddText2ImageBlazorComponents();

    [Fact]
    public void BackendSelector_RendersAvailableProviders()
    {
        var state = Services.GetRequiredService<Text2ImageState>();
        state.Backend = ExecutionProvider.Cpu;

        var cut = RenderComponent<BackendSelector>(parameters => parameters
            .Add(p => p.AvailableProviders, new HashSet<ExecutionProvider> { ExecutionProvider.Auto, ExecutionProvider.Cpu })
            .Add(p => p.ShowAvailabilityBadge, true));

        Assert.Contains("Execution provider", cut.Markup);
        Assert.Equal(4, cut.FindAll("input[type=radio]").Count);
        Assert.Contains("unavailable", cut.Markup);
    }

    [Fact]
    public void BackendSelector_ChangesStateAndInvokesCallback()
    {
        ExecutionProvider? selected = null;
        var cut = RenderComponent<BackendSelector>(parameters => parameters
            .Add(p => p.AvailableProviders, new HashSet<ExecutionProvider> { ExecutionProvider.Auto, ExecutionProvider.Cpu, ExecutionProvider.Cuda })
            .Add(p => p.OnBackendChanged, EventCallback.Factory.Create<ExecutionProvider>(this, value => selected = value)));

        var cuda = cut.FindAll("input[type=radio]").Single(x => x.GetAttribute("value") == "Cuda");
        cuda.Change("Cuda");

        Assert.Equal(ExecutionProvider.Cuda, selected);
        Assert.Equal(ExecutionProvider.Cuda, Services.GetRequiredService<Text2ImageState>().Backend);
    }

    [Fact]
    public void PromptEditor_RendersDefaultsAndDisablesEmptyPrompt()
    {
        var cut = RenderComponent<PromptEditor>(parameters => parameters
            .Add(p => p.DefaultGuidanceScale, 8.5)
            .Add(p => p.DefaultSteps, 30));

        Assert.Contains("Guidance: 8.5", cut.Markup);
        Assert.Contains("Steps: 30", cut.Markup);
        Assert.True(cut.Find("button.btn-primary").HasAttribute("disabled"));
    }

    [Fact]
    public void PromptEditor_SubmitsGenerationRequest()
    {
        GenerationRequest? request = null;
        var cut = RenderComponent<PromptEditor>(parameters => parameters
            .Add(p => p.OnGenerate, EventCallback.Factory.Create<GenerationRequest>(this, value => request = value)));

        cut.Find("textarea").Change("a mountain cabin");
        cut.Find("button.btn-primary").Click();

        Assert.NotNull(request);
        Assert.Equal("a mountain cabin", request!.Prompt);
        Assert.Equal(20, request.Options.NumInferenceSteps);
        Assert.Equal(string.Empty, request.NegativePrompt);
    }

    [Fact]
    public void GeneratedImageGallery_RendersEmptyState()
    {
        var cut = RenderComponent<GeneratedImageGallery>();

        Assert.Contains("No generated images yet.", cut.Markup);
        Assert.Empty(cut.FindAll("img"));
    }

    [Fact]
    public void GeneratedImageGallery_OpensAndClosesImageModal()
    {
        var image = NewImage("sunset");
        var cut = RenderComponent<GeneratedImageGallery>(parameters => parameters
            .Add(p => p.Images, new[] { image }));

        cut.Find("button").Click();
        Assert.Single(cut.FindAll(".t2i-modal"));

        cut.Find(".t2i-modal button").Click();
        Assert.Empty(cut.FindAll(".t2i-modal"));
    }

    [Fact]
    public void ImageCaptionViewer_RendersImageCaptionAndAction()
    {
        var cut = RenderComponent<ImageCaptionViewer>(parameters => parameters
            .Add(p => p.ImageUrl, "data:image/png;base64,AA==")
            .Add(p => p.Caption, "A red square")
            .Add(p => p.OnRecaption, EventCallback.Factory.Create<string>(this, _ => Task.CompletedTask)));

        Assert.Equal("A red square", cut.Find("p").TextContent);
        Assert.Equal("Re-Caption", cut.Find("button").TextContent);
    }

    [Fact]
    public void ImageCaptionViewer_InvokesRecaptionWithImageUrl()
    {
        string? imageUrl = null;
        var cut = RenderComponent<ImageCaptionViewer>(parameters => parameters
            .Add(p => p.ImageUrl, "https://example.test/image.png")
            .Add(p => p.OnRecaption, EventCallback.Factory.Create<string>(this, value => imageUrl = value)));

        cut.Find("button").Click();

        Assert.Equal("https://example.test/image.png", imageUrl);
    }

    [Fact]
    public void InferenceProgressBar_RendersProgressAndEtaFromState()
    {
        var state = Services.GetRequiredService<Text2ImageState>();
        state.SetProgress(new InferenceProgress(25, 100, TimeSpan.FromSeconds(12)));
        var cut = RenderComponent<InferenceProgressBar>();

        Assert.Equal("25 / 100", cut.Find(".progress-bar").TextContent);
        Assert.Contains("About 12s remaining", cut.Markup);

        state.SetProgress(new InferenceProgress(null, null));
        Assert.Equal("Generating…", cut.Find(".progress-bar").TextContent);
    }

    [Fact]
    public void AddText2ImageBlazorComponents_RegistersScopedState()
    {
        using var provider = Services.BuildServiceProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var first = scope1.ServiceProvider.GetRequiredService<Text2ImageState>();
        var same = scope1.ServiceProvider.GetRequiredService<Text2ImageState>();
        var other = scope2.ServiceProvider.GetRequiredService<Text2ImageState>();

        Assert.Same(first, same);
        Assert.NotSame(first, other);
    }

    [Fact]
    public void Text2ImageState_NotifiesAndStoresValues()
    {
        var state = new Text2ImageState();
        var changes = 0;
        state.Changed += () => changes++;
        var images = new[] { NewImage("test") };

        state.SetImages(images);
        state.SetProgress(new InferenceProgress(1, 2));

        Assert.Same(images, state.Images);
        Assert.Equal(new InferenceProgress(1, 2), state.Progress);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void GeneratedImageGallery_ClampsInvalidColumnCount()
    {
        var cut = RenderComponent<GeneratedImageGallery>(parameters => parameters
            .Add(p => p.Images, new[] { NewImage("edge") })
            .Add(p => p.Columns, 0));

        Assert.Contains("col-md-12", cut.Markup);
    }

    private static ImageGenerationResult NewImage(string prompt) => new()
    {
        ImageBytes = new byte[] { 0, 1, 2 },
        ModelName = "test-model",
        Prompt = prompt,
        Seed = 42,
        InferenceTimeMs = 10
    };
}
