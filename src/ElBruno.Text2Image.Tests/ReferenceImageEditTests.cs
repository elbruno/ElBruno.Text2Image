using System.Net;
using System.Text;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Xunit;

namespace ElBruno.Text2Image.Tests;

internal static class ReferenceImageTestData
{
    public static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];
    public static readonly byte[] Webp = [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P'];

    public static string PngDataUri => ReferenceImageHelper.ToDataUri(Png, "image/png");
    public static string JpegDataUri => ReferenceImageHelper.ToDataUri(Jpeg, "image/jpeg");
    public static string WebpDataUri => ReferenceImageHelper.ToDataUri(Webp, "image/webp");
}

public class ReferenceImageHelperTests
{
    [Fact]
    public void DetectMediaType_RecognizesSignatures()
    {
        Assert.Equal("image/png", ReferenceImageHelper.DetectMediaType(ReferenceImageTestData.Png));
        Assert.Equal("image/jpeg", ReferenceImageHelper.DetectMediaType(ReferenceImageTestData.Jpeg));
        Assert.Equal("image/webp", ReferenceImageHelper.DetectMediaType(ReferenceImageTestData.Webp));
        Assert.Null(ReferenceImageHelper.DetectMediaType("GIF89a"u8.ToArray()));
    }

    [Fact]
    public void LoadFileAsDataUri_ValidPng_ReturnsDataUri()
    {
        var path = Path.Combine(Path.GetTempPath(), $"t2i-ref-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, ReferenceImageTestData.Png);
        try
        {
            var uri = ReferenceImageHelper.LoadFileAsDataUri(path);
            Assert.StartsWith("data:image/png;base64,", uri);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadFileAsDataUri_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => ReferenceImageHelper.LoadFileAsDataUri(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png")));
    }

    [Fact]
    public void LoadFileAsDataUri_UnknownFormat_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"t2i-ref-{Guid.NewGuid():N}.png");
        File.WriteAllText(path, "not an image");
        try
        {
            Assert.Throws<ArgumentException>(() => ReferenceImageHelper.LoadFileAsDataUri(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LoadFileAsDataUri_TooLarge_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"t2i-ref-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, ReferenceImageTestData.Png);
        try
        {
            Assert.Throws<ArgumentException>(() => ReferenceImageHelper.LoadFileAsDataUri(path, maxBytes: 4));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ResolveAsync_DataUri_DecodesBytes()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => throw new InvalidOperationException("no network expected")));
        var (bytes, type) = await ReferenceImageHelper.ResolveAsync(ReferenceImageTestData.JpegDataUri, http);
        Assert.Equal("image/jpeg", type);
        Assert.Equal(ReferenceImageTestData.Jpeg, bytes);
    }

    [Fact]
    public async Task ResolveAsync_HttpsUrl_DownloadsWithoutApiKey()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(ReferenceImageTestData.Png) });
        using var http = new HttpClient(handler);
        var (bytes, type) = await ReferenceImageHelper.ResolveAsync("https://example.com/cat.png", http);
        Assert.Equal("image/png", type);
        Assert.Equal(ReferenceImageTestData.Png.Length, bytes.Length);
        Assert.False(handler.LastRequest!.Headers.Contains("api-key"));
    }

    [Fact]
    public async Task ResolveAsync_HttpUrl_Throws()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        await Assert.ThrowsAsync<ArgumentException>(() => ReferenceImageHelper.ResolveAsync("http://example.com/cat.png", http));
    }

    [Fact]
    public void SetMaskImageFromFile_RejectsNonPng()
    {
        var path = Path.Combine(Path.GetTempPath(), $"t2i-mask-{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(path, ReferenceImageTestData.Jpeg);
        try
        {
            var options = new ImageGenerationOptions();
            Assert.Throws<ArgumentException>(() => options.SetMaskImageFromFile(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MeaiConverter_MapsMaskAndFidelity()
    {
        var meai = new Microsoft.Extensions.AI.ImageGenerationOptions
        {
            AdditionalProperties = new()
            {
                [Text2ImagePropertyNames.MaskImage] = "data:image/png;base64,AAAA",
                [Text2ImagePropertyNames.InputFidelity] = "high"
            }
        };
        var options = ImageGenerationOptionsConverter.FromMeaiOptions(meai);
        Assert.Equal("data:image/png;base64,AAAA", options.MaskImage);
        Assert.Equal("high", options.InputFidelity);
    }

    [Fact]
    public void ApplyEditInputs_MapsOriginalImages()
    {
        var request = new Microsoft.Extensions.AI.ImageGenerationRequest("edit", [new Microsoft.Extensions.AI.DataContent(ReferenceImageTestData.Png, "image/png")]);
        var options = new ImageGenerationOptions();
        ImageGenerationOptionsConverter.ApplyEditInputs(options, request, null);
        Assert.Single(options.ReferenceImages!);
        Assert.StartsWith("data:image/png;base64,", options.ReferenceImages![0]);
    }
}

public class GptImageEditTests
{
    private static string CaptureBody(FakeHttpHandler handler) => handler.LastRequestBody ?? string.Empty;

    [Fact]
    public async Task GptImage2_WithReferenceImages_CallsEditsEndpointWithMultipart()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var http = new HttpClient(handler);
        using var generator = new GptImage2Generator("https://res.openai.azure.com", "test-key", http, deploymentName: "gpt-image-2");

        var result = await generator.GenerateAsync("make it blue", new ImageGenerationOptions
        {
            ReferenceImages = [ReferenceImageTestData.PngDataUri, ReferenceImageTestData.JpegDataUri],
            MaskImage = ReferenceImageTestData.PngDataUri,
            InputFidelity = "High",
            Width = 1536,
            Height = 1024
        });

        var request = handler.LastRequest!;
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/openai/deployments/gpt-image-2/images/edits", request.RequestUri!.AbsolutePath);
        Assert.Contains("api-version=", request.RequestUri.Query);
        Assert.True(request.Headers.TryGetValues("api-key", out var keys));
        Assert.Equal("test-key", keys.Single());
        Assert.Equal("multipart/form-data", request.Content!.Headers.ContentType!.MediaType);

        var body = CaptureBody(handler);
        Assert.Equal(2, CountOccurrences(body, "name=\"image[]\""));
        Assert.Contains("name=mask", body.Replace("\"", string.Empty));
        Assert.Contains("name=input_fidelity", body.Replace("\"", string.Empty));
        Assert.Contains("high", body);
        Assert.Contains("1536x1024", body);
        Assert.Contains("make it blue", body);
        Assert.Equal(1536, result.Width);
        Assert.Equal(1024, result.Height);
        Assert.NotEmpty(result.ImageBytes);
    }

    [Fact]
    public async Task GptImage1p5_WithReferenceImage_CallsEditsEndpoint()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var http = new HttpClient(handler);
        using var generator = new GptImage1p5Generator("https://res.openai.azure.com/openai/v1", "k", http, deploymentName: "gpt-image-1.5");

        await generator.GenerateAsync("edit", new ImageGenerationOptions { ReferenceImages = [ReferenceImageTestData.PngDataUri] });

        Assert.Equal("res.openai.azure.com", handler.LastRequest!.RequestUri!.Host);
        Assert.EndsWith("/deployments/gpt-image-1.5/images/edits", handler.LastRequest.RequestUri.AbsolutePath);
        Assert.DoesNotContain("name=mask", CaptureBody(handler).Replace("\"", string.Empty));
    }

    [Fact]
    public async Task GptImage25_WithReferenceImage_CallsEditsEndpoint()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var http = new HttpClient(handler);
        using var generator = new GptImage25Generator("https://res.openai.azure.com", "k", http, deploymentName: "gpt-image-2.5-flare");

        await generator.GenerateAsync("edit", new ImageGenerationOptions { ReferenceImages = [ReferenceImageTestData.WebpDataUri] });

        Assert.EndsWith("/deployments/gpt-image-2.5-flare/images/edits", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Contains("image/webp", CaptureBody(handler));
    }

    [Fact]
    public async Task GptImage2_TooManyReferenceImages_Throws()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse()));
        using var generator = new GptImage2Generator("https://res.openai.azure.com", "k", http);
        var images = Enumerable.Repeat(ReferenceImageTestData.PngDataUri, 17).ToList();

        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateAsync("edit", new ImageGenerationOptions { ReferenceImages = images }));
    }

    [Fact]
    public async Task GptImage2_InvalidInputFidelity_Throws()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse()));
        using var generator = new GptImage2Generator("https://res.openai.azure.com", "k", http);

        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateAsync("edit", new ImageGenerationOptions
        {
            ReferenceImages = [ReferenceImageTestData.PngDataUri],
            InputFidelity = "ultra"
        }));
    }

    [Fact]
    public async Task GptImage2_JpegMask_Throws()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse()));
        using var generator = new GptImage2Generator("https://res.openai.azure.com", "k", http);

        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateAsync("edit", new ImageGenerationOptions
        {
            ReferenceImages = [ReferenceImageTestData.PngDataUri],
            MaskImage = ReferenceImageTestData.JpegDataUri
        }));
    }

    [Fact]
    public async Task GptImage2_MaskWithoutReferenceImages_Throws()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse()));
        using var generator = new GptImage2Generator("https://res.openai.azure.com", "k", http);

        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateAsync("edit", new ImageGenerationOptions { MaskImage = ReferenceImageTestData.PngDataUri }));
    }

    [Fact]
    public async Task GptImage2_EditError_SurfacesStatusAndMessage()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => GptImage2FakeResponses.CreateErrorResponse(HttpStatusCode.BadRequest, "bad image")));
        using var generator = new GptImage2Generator("https://res.openai.azure.com", "k", http);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => generator.GenerateAsync("edit", new ImageGenerationOptions { ReferenceImages = [ReferenceImageTestData.PngDataUri] }));
        Assert.Contains("400", ex.Message);
        Assert.Contains("bad image", ex.Message);
    }

    [Fact]
    public async Task GptImage2_WithoutReferenceImages_UsesGenerationsEndpoint()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var http = new HttpClient(handler);
        using var generator = new GptImage2Generator("https://res.openai.azure.com", "k", http);

        await generator.GenerateAsync("plain prompt");

        Assert.EndsWith("/images/generations", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    private static int CountOccurrences(string text, string value)
    {
        int count = 0, index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0) { count++; index += value.Length; }
        return count;
    }
}

public class MaiImage25EditTests
{
    [Fact]
    public async Task WithReferenceImages_CallsMaiEditsEndpointWithMultipart()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var http = new HttpClient(handler);
        using var generator = new MaiImage25Generator("https://res.services.ai.azure.com", "k", http, modelId: "MAI-Image-2.5-Flash");

        await generator.GenerateAsync("edit", new ImageGenerationOptions
        {
            ReferenceImages = [ReferenceImageTestData.PngDataUri, ReferenceImageTestData.JpegDataUri]
        });

        var request = handler.LastRequest!;
        Assert.Equal("/mai/v1/images/edits", request.RequestUri!.AbsolutePath);
        Assert.Equal("multipart/form-data", request.Content!.Headers.ContentType!.MediaType);
        var body = handler.LastRequestBody!.Replace("\"", string.Empty);
        Assert.Equal(2, body.Split("name=image;").Length - 1);
        Assert.Contains("name=model", body);
        Assert.Contains("MAI-Image-2.5-Flash", body);
        Assert.Contains("name=prompt", body);
    }

    [Fact]
    public void EditsEndpoint_DerivedFromFullGenerationsUrl()
    {
        using var http = new HttpClient();
        using var generator = new MaiImage25Generator("https://res.services.ai.azure.com/mai/v1/images/generations", "k", http);
        Assert.Equal("https://res.services.ai.azure.com/mai/v1/images/edits", generator.EditsEndpoint);
    }

    [Fact]
    public async Task WithMask_ThrowsNotSupported()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse()));
        using var generator = new MaiImage25Generator("https://res.services.ai.azure.com", "k", http);

        await Assert.ThrowsAsync<NotSupportedException>(() => generator.GenerateAsync("edit", new ImageGenerationOptions
        {
            ReferenceImages = [ReferenceImageTestData.PngDataUri],
            MaskImage = ReferenceImageTestData.PngDataUri
        }));
    }

    [Fact]
    public async Task TooManyReferenceImages_Throws()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse()));
        using var generator = new MaiImage25Generator("https://res.services.ai.azure.com", "k", http);

        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateAsync("edit", new ImageGenerationOptions
        {
            ReferenceImages = Enumerable.Repeat(ReferenceImageTestData.PngDataUri, 6).ToList()
        }));
    }

    [Fact]
    public async Task WebpReference_Throws()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse()));
        using var generator = new MaiImage25Generator("https://res.services.ai.azure.com", "k", http);

        await Assert.ThrowsAsync<ArgumentException>(() => generator.GenerateAsync("edit", new ImageGenerationOptions
        {
            ReferenceImages = [ReferenceImageTestData.WebpDataUri]
        }));
    }

    [Fact]
    public async Task WithoutReferenceImages_UsesGenerationsEndpointWithJson()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var http = new HttpClient(handler);
        using var generator = new MaiImage25Generator("https://res.services.ai.azure.com", "k", http);

        await generator.GenerateAsync("plain");

        Assert.Equal("/mai/v1/images/generations", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("application/json", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
    }
}

public class Flux2MaskTests
{
    [Fact]
    public async Task WithMask_ThrowsNotSupported()
    {
        using var http = new HttpClient(new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse()));
        using var generator = new Flux2Generator("https://res.services.ai.azure.com", "k", http);

        await Assert.ThrowsAsync<NotSupportedException>(() => generator.GenerateAsync("edit", new ImageGenerationOptions
        {
            ReferenceImages = [ReferenceImageTestData.PngDataUri],
            MaskImage = ReferenceImageTestData.PngDataUri
        }));
    }
}
