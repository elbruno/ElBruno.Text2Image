namespace ElBruno.Text2Image;

/// <summary>
/// Helpers for loading, validating and resolving reference images used by image-to-image / edit flows.
/// </summary>
public static class ReferenceImageHelper
{
    /// <summary>Maximum supported size for a single reference image (50 MB).</summary>
    public const long MaxReferenceImageBytes = 50L * 1024 * 1024;

    /// <summary>Maximum supported size for a mask image (4 MB).</summary>
    public const long MaxMaskImageBytes = 4L * 1024 * 1024;

    /// <summary>
    /// Detects the image media type from the file signature (magic bytes).
    /// Returns <c>image/png</c>, <c>image/jpeg</c>, <c>image/webp</c>, or <c>null</c> when unknown.
    /// </summary>
    public static string? DetectMediaType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            return "image/png";

        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";

        if (bytes.Length >= 12 &&
            bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
            return "image/webp";

        return null;
    }

    /// <summary>Returns the default file extension (including dot) for a supported media type.</summary>
    public static string GetFileExtension(string mediaType) => mediaType switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/webp" => ".webp",
        _ => ".bin"
    };

    /// <summary>Builds a base64 Data URI for the given bytes.</summary>
    public static string ToDataUri(byte[] bytes, string mediaType)
        => $"data:{mediaType};base64,{Convert.ToBase64String(bytes)}";

    /// <summary>
    /// Loads a local image file, validates its format (PNG/JPEG/WebP) and size, and returns a Data URI.
    /// </summary>
    /// <param name="filePath">Local file path.</param>
    /// <param name="maxBytes">Maximum allowed file size in bytes.</param>
    /// <param name="allowedMediaTypes">Optional allowed media types; defaults to PNG, JPEG and WebP.</param>
    public static string LoadFileAsDataUri(string filePath, long maxBytes = MaxReferenceImageBytes, IReadOnlyCollection<string>? allowedMediaTypes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var info = new FileInfo(filePath);
        if (!info.Exists)
            throw new FileNotFoundException($"Image file not found: {filePath}", filePath);
        if (info.Length == 0)
            throw new ArgumentException($"Image file is empty: {filePath}", nameof(filePath));
        if (info.Length > maxBytes)
            throw new ArgumentException($"Image file '{filePath}' is {info.Length / (1024.0 * 1024.0):F1} MB, which exceeds the {maxBytes / (1024 * 1024)} MB limit.", nameof(filePath));

        var bytes = File.ReadAllBytes(filePath);
        var mediaType = DetectMediaType(bytes)
            ?? throw new ArgumentException($"Unsupported image format for '{filePath}'. Use PNG, JPEG, or WebP.", nameof(filePath));
        if (allowedMediaTypes is not null && !allowedMediaTypes.Contains(mediaType))
            throw new ArgumentException($"Image '{filePath}' is {mediaType}, but only {string.Join(", ", allowedMediaTypes)} is supported here.", nameof(filePath));

        return ToDataUri(bytes, mediaType);
    }

    /// <summary>
    /// Resolves a reference image (Data URI, raw base64 string, or HTTPS URL) into raw bytes and media type.
    /// HTTPS URLs are downloaded with the supplied <paramref name="httpClient"/> without any credentials.
    /// </summary>
    public static async Task<(byte[] Bytes, string MediaType)> ResolveAsync(
        string image, HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(image);
        ArgumentNullException.ThrowIfNull(httpClient);

        byte[] bytes;
        string? declaredType = null;

        if (image.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = image.IndexOf(',');
            if (comma < 0 || !image.AsSpan(0, comma).Contains(";base64".AsSpan(), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Reference image Data URI must be base64-encoded.");
            declaredType = image[5..comma].Split(';')[0];
            bytes = DecodeBase64(image[(comma + 1)..]);
        }
        else if (Uri.TryCreate(image, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            if (uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Reference image URLs must use HTTPS.");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Failed to download reference image from '{uri.GetLeftPart(UriPartial.Path)}' (HTTP {(int)response.StatusCode}).");
            if (response.Content.Headers.ContentLength > MaxReferenceImageBytes)
                throw new ArgumentException("Reference image download exceeds the 50 MB limit.");
            bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            declaredType = response.Content.Headers.ContentType?.MediaType;
        }
        else
        {
            bytes = DecodeBase64(image);
        }

        if (bytes.LongLength > MaxReferenceImageBytes)
            throw new ArgumentException("Reference image exceeds the 50 MB limit.");

        var mediaType = DetectMediaType(bytes) ?? declaredType;
        if (mediaType is not ("image/png" or "image/jpeg" or "image/webp"))
            throw new ArgumentException("Unsupported reference image format. Use PNG, JPEG, or WebP.");

        return (bytes, mediaType);
    }

    private static byte[] DecodeBase64(string value)
    {
        try
        {
            return Convert.FromBase64String(value.Trim());
        }
        catch (FormatException)
        {
            throw new ArgumentException("Reference image must be an HTTPS URL, a base64 Data URI, or a base64-encoded string.");
        }
    }
}
