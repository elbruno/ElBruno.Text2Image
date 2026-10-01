using ElBruno.Text2Image.Cli.Providers;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Validates and loads <c>--image</c>, <c>--mask</c> and <c>--input-fidelity</c> CLI inputs
/// against a provider's <see cref="ReferenceImageCapabilities"/>.
/// </summary>
internal static class ReferenceImageInputs
{
    internal sealed record Prepared(IReadOnlyList<string>? Images, string? Mask, string? InputFidelity);

    /// <summary>
    /// Validates inputs and converts local files into Data URIs. Returns an error message on failure.
    /// </summary>
    public static (Prepared? Result, string? Error) Prepare(
        IProviderAdapter provider,
        IReadOnlyList<string>? images,
        string? mask,
        string? inputFidelity)
    {
        var imageList = images?.Where(i => !string.IsNullOrWhiteSpace(i)).ToList() ?? [];
        var hasMask = !string.IsNullOrWhiteSpace(mask);
        var hasFidelity = !string.IsNullOrWhiteSpace(inputFidelity);

        if (imageList.Count == 0)
        {
            if (hasMask)
                return (null, "--mask requires at least one --image.");
            if (hasFidelity)
                return (null, "--input-fidelity requires at least one --image.");
            return (new Prepared(null, null, null), null);
        }

        var caps = provider.ReferenceImageSupport;
        if (caps is null)
            return (null, $"Provider '{provider.Id}' does not support reference images. Use foundry-flux2, foundry-mai25, foundry-mai25-flash, or a foundry-gpt-image-* provider.");

        if (imageList.Count > caps.MaxImages)
            return (null, $"Provider '{provider.Id}' accepts at most {caps.MaxImages} reference image(s); got {imageList.Count}.");

        if (hasMask && !caps.SupportsMask)
            return (null, $"Provider '{provider.Id}' does not support --mask. Use a foundry-gpt-image-* provider for masked edits.");

        string? fidelity = null;
        if (hasFidelity)
        {
            if (!caps.SupportsInputFidelity)
                return (null, $"Provider '{provider.Id}' does not support --input-fidelity.");
            fidelity = inputFidelity!.Trim().ToLowerInvariant();
            if (fidelity is not ("low" or "high"))
                return (null, "--input-fidelity must be 'low' or 'high'.");
        }

        var resolved = new List<string>(imageList.Count);
        foreach (var image in imageList)
        {
            var (value, error) = ResolveImage(image, caps.AllowedMediaTypes, ReferenceImageHelper.MaxReferenceImageBytes, "--image");
            if (error is not null)
                return (null, error);
            resolved.Add(value!);
        }

        string? resolvedMask = null;
        if (hasMask)
        {
            var (value, error) = ResolveImage(mask!, ["image/png"], ReferenceImageHelper.MaxMaskImageBytes, "--mask");
            if (error is not null)
                return (null, error);
            resolvedMask = value;
        }

        return (new Prepared(resolved, resolvedMask, fidelity), null);
    }

    private static (string? Value, string? Error) ResolveImage(string input, IReadOnlyList<string> allowedTypes, long maxBytes, string optionName)
    {
        input = input.Trim();

        if (input.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var semi = input.IndexOfAny([';', ',']);
            var mediaType = semi > 5 ? input[5..semi].ToLowerInvariant() : string.Empty;
            if (!allowedTypes.Contains(mediaType))
                return (null, $"{optionName} data URI type '{mediaType}' is not supported. Allowed: {string.Join(", ", allowedTypes)}.");
            return (input, null);
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            if (uri.Scheme != Uri.UriSchemeHttps)
                return (null, $"{optionName} URLs must use HTTPS: {uri.GetLeftPart(UriPartial.Path)}");
            return (input, null);
        }

        if (!File.Exists(input))
            return (null, $"{optionName} file not found: {input}");

        try
        {
            return (ReferenceImageHelper.LoadFileAsDataUri(input, maxBytes, allowedTypes.ToArray()), null);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return (null, $"{optionName}: {ex.Message}");
        }
    }
}
