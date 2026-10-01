using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using OpenAI.Images;

namespace ElBruno.Text2Image.Foundry;

internal static class GptImageSupport
{
    internal const string EditsApiVersion = "2025-04-01-preview";
    internal const int MaxEditImages = 16;

    public static (GeneratedImageSize Size, int Width, int Height) MapSize(int width, int height)
    {
        var requestedWidth = width > 0 ? width : 1024;
        var requestedHeight = height > 0 ? height : 1024;
        var aspectRatio = (double)requestedWidth / requestedHeight;

        if (aspectRatio > 1.2)
        {
            return (GeneratedImageSize.W1792xH1024, 1792, 1024);
        }

        if (aspectRatio < 0.85)
        {
            return (GeneratedImageSize.W1024xH1792, 1024, 1792);
        }

        return (GeneratedImageSize.W1024xH1024, 1024, 1024);
    }

    /// <summary>Maps requested dimensions to a size supported by the GPT-Image edits API.</summary>
    public static (string Size, int Width, int Height) MapEditSize(int width, int height)
    {
        var requestedWidth = width > 0 ? width : 1024;
        var requestedHeight = height > 0 ? height : 1024;
        var aspectRatio = (double)requestedWidth / requestedHeight;

        if (aspectRatio > 1.2) return ("1536x1024", 1536, 1024);
        if (aspectRatio < 0.85) return ("1024x1536", 1024, 1536);
        return ("1024x1024", 1024, 1024);
    }

    /// <summary>
    /// Calls the Azure OpenAI <c>images/edits</c> endpoint with the reference images (and optional mask)
    /// from <paramref name="options"/>.
    /// </summary>
    public static async Task<ImageGenerationResult> EditAsync(
        HttpClient httpClient,
        string endpoint,
        string deploymentName,
        string apiKey,
        string modelDisplayName,
        string prompt,
        ImageGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var references = options.ReferenceImages!;
        if (references.Count > MaxEditImages)
            throw new ArgumentException($"{modelDisplayName} supports at most {MaxEditImages} reference images (got {references.Count}).", nameof(options));

        string? fidelity = null;
        if (!string.IsNullOrWhiteSpace(options.InputFidelity))
        {
            fidelity = options.InputFidelity.Trim().ToLowerInvariant();
            if (fidelity is not ("low" or "high"))
                throw new ArgumentException("InputFidelity must be 'low' or 'high'.", nameof(options));
        }

        var mapped = MapEditSize(options.Width, options.Height);
        var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{Uri.EscapeDataString(deploymentName)}/images/edits?api-version={EditsApiVersion}";

        using var content = new MultipartFormDataContent();
        var index = 0;
        foreach (var reference in references)
        {
            var (bytes, mediaType) = await ReferenceImageHelper.ResolveAsync(reference, httpClient, cancellationToken).ConfigureAwait(false);
            if (mediaType is not ("image/png" or "image/jpeg" or "image/webp"))
                throw new ArgumentException($"{modelDisplayName} edits accept PNG, JPEG, or WebP images.");
            var part = new ByteArrayContent(bytes);
            part.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            content.Add(part, "image[]", $"image{index++}{ReferenceImageHelper.GetFileExtension(mediaType)}");
        }

        if (!string.IsNullOrWhiteSpace(options.MaskImage))
        {
            var (maskBytes, maskType) = await ReferenceImageHelper.ResolveAsync(options.MaskImage, httpClient, cancellationToken).ConfigureAwait(false);
            if (maskType != "image/png")
                throw new ArgumentException("Mask image must be a PNG file.", nameof(options));
            if (maskBytes.LongLength > ReferenceImageHelper.MaxMaskImageBytes)
                throw new ArgumentException("Mask image must be smaller than 4 MB.", nameof(options));
            var maskPart = new ByteArrayContent(maskBytes);
            maskPart.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(maskPart, "mask", "mask.png");
        }

        content.Add(new StringContent(prompt), "prompt");
        content.Add(new StringContent(mapped.Size), "size");
        content.Add(new StringContent("1"), "n");
        if (fidelity is not null)
            content.Add(new StringContent(fidelity), "input_fidelity");

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.TryAddWithoutValidation("api-key", apiKey);

        var sw = Stopwatch.StartNew();
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        sw.Stop();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{modelDisplayName} image edit failed with HTTP {(int)response.StatusCode} ({response.StatusCode}). {ExtractErrorMessage(body)}".TrimEnd());

        var imageBytes = ExtractImageBytes(body)
            ?? throw new InvalidOperationException($"{modelDisplayName} image edit response did not contain image data.");

        return new ImageGenerationResult
        {
            ImageBytes = imageBytes,
            ModelName = modelDisplayName,
            Prompt = prompt,
            Seed = options.Seed ?? 0,
            Width = mapped.Width,
            Height = mapped.Height,
            InferenceTimeMs = sw.ElapsedMilliseconds
        };
    }

    internal static byte[]? ExtractImageBytes(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0 &&
                data[0].TryGetProperty("b64_json", out var b64) && b64.GetString() is { Length: > 0 } s)
            {
                return Convert.FromBase64String(s);
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }

    internal static string ExtractErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var msg))
                    return msg.GetString() ?? string.Empty;
                if (error.ValueKind == JsonValueKind.String)
                    return error.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
        }
        return string.Empty;
    }
}
