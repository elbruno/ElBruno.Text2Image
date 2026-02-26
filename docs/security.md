# Security Considerations

This document describes the security hardening applied to the **ElBruno.Text2Image** library and the reasoning behind each measure. If you are contributing to or extending this library, please follow these patterns.

---

## 1. Thread Safety — Double-Check Locking on Pipeline Initialization

**Risk:** Under concurrent access, multiple threads could simultaneously create `StableDiffusionPipeline` instances, leaking unmanaged ONNX Runtime sessions and causing resource exhaustion.

**Mitigation:** All four local generators (`StableDiffusion15`, `StableDiffusion21`, `SdxlTurbo`, `LcmDreamshaperV7`) use the double-check locking pattern to ensure the pipeline is only created once, even under concurrent calls:

```csharp
private readonly object _pipelineLock = new();
private StableDiffusionPipeline? _pipeline;

// Inside GenerateAsync:
if (_pipeline == null)
{
    lock (_pipelineLock)
    {
        if (_pipeline == null)
        {
            var sessionOptions = SessionOptionsHelper.Create(options.ExecutionProvider);
            _pipeline = new StableDiffusionPipeline(modelPath, sessionOptions, EmbeddingDim);
        }
    }
}
```

**Why not `Lazy<T>`?** The pipeline creation depends on runtime options (execution provider, model path) that come from the caller, so a simple `Lazy<T>` with a fixed factory is not sufficient.

---

## 2. SSRF Mitigation — Image URL Fetch Without Credentials

**Risk:** The FLUX.2 cloud API may return an image URL in its response. If the library blindly follows that URL with the API key attached, a malicious or compromised server could redirect to an attacker-controlled endpoint and capture the credential (Server-Side Request Forgery / credential leakage).

**Mitigation:** When downloading from a server-returned URL, a separate `HttpRequestMessage` is used **without** the `api-key` header:

```csharp
// Use a separate request WITHOUT the API key to avoid credential leakage (SSRF mitigation)
using var imageRequest = new HttpRequestMessage(HttpMethod.Get, imageData.Url);
var imageResponse = await _httpClient.SendAsync(imageRequest, cancellationToken);
```

The API key is only ever sent to the configured endpoint, never to URLs returned in API responses.

---

## 3. API Key Isolation — Per-Request Headers

**Risk:** Adding the API key to `HttpClient.DefaultRequestHeaders` mutates shared state. If a user provides a shared or pooled `HttpClient` (e.g., from `IHttpClientFactory`), the key would leak to every other request made by that client in the application.

**Mitigation:** The API key is attached per-request via `HttpRequestMessage.Headers`, not via `DefaultRequestHeaders`:

```csharp
using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
request.Headers.TryAddWithoutValidation("api-key", _apiKey);
request.Content = JsonContent.Create(requestBody, Flux2JsonContext.Default.Flux2Request);

var response = await _httpClient.SendAsync(request, cancellationToken);
```

This is safe for shared, pooled, or singleton `HttpClient` instances.

---

## 4. Error Information Disclosure — Truncated Error Bodies

**Risk:** When the FLUX.2 API returns an error, the full response body could contain internal server details, stack traces, or other sensitive information. Propagating unbounded error text in exceptions could expose this data to logs, monitoring, or end users.

**Mitigation:** Error response bodies are truncated to a maximum of 1,024 characters before being included in exception messages:

```csharp
private const int MaxErrorBodyLength = 1024;

var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
if (errorBody.Length > MaxErrorBodyLength)
    errorBody = errorBody[..MaxErrorBodyLength] + "... (truncated)";

throw new HttpRequestException(
    $"FLUX.2 API returned {response.StatusCode}: {errorBody}");
```

---

## 5. Input Validation — Width and Height Bounds

**Risk:** Accepting arbitrary width/height values could lead to:
- **Denial of service** via unbounded memory allocation (e.g., `Width = int.MaxValue`)
- **ONNX Runtime crashes** from dimensions that aren't multiples of 8 (required by the model architecture)

**Mitigation:** The `ImageGenerationOptions.Width` and `ImageGenerationOptions.Height` properties enforce strict validation in their setters:

```csharp
public int Width
{
    get => _width;
    set
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 64);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 4096);
        if (value % 8 != 0)
            throw new ArgumentException("Width must be a multiple of 8.", nameof(value));
        _width = value;
    }
}
```

| Constraint | Value | Reason |
|---|---|---|
| Minimum | 64 px | Smallest dimension that produces meaningful results |
| Maximum | 4,096 px | Prevents excessive memory allocation (~4K is max for most SD models) |
| Multiple of 8 | Required | ONNX model tensor dimensions require this alignment |

---

## 6. Resource Disposal — SessionOptions Lifecycle

**Risk:** `StableDiffusionPipeline` creates two `SessionOptions` objects (one for GPU/preferred provider, one CPU fallback for the tokenizer). If these are not disposed, ONNX Runtime native memory leaks over time.

**Mitigation:** Both `SessionOptions` instances are stored as fields and disposed in the `Dispose()` method:

```csharp
internal sealed class StableDiffusionPipeline : IDisposable
{
    private readonly SessionOptions _sessionOptions;
    private readonly SessionOptions _cpuOptions;

    // ...

    public void Dispose()
    {
        _textEncoder.Dispose();
        _unet.Dispose();
        _vaeDecoder.Dispose();
        _sessionOptions.Dispose();
        _cpuOptions.Dispose();
    }
}
```

All generators that own a pipeline also implement `IDisposable` and call `_pipeline?.Dispose()`.

---

## Best Practices for Contributors

1. **Never add credentials to `DefaultRequestHeaders`** — always use per-request `HttpRequestMessage.Headers`.
2. **Never follow server-returned URLs with credentials** — treat redirected URLs as untrusted.
3. **Always validate user-provided dimensions** — enforce bounds and alignment constraints early.
4. **Always dispose ONNX sessions** — track and dispose all `SessionOptions` and `InferenceSession` objects.
5. **Use double-check locking** for lazy-initialized shared resources (pipeline, sessions).
6. **Truncate external data** before including in exception messages or logs.
7. **Use `CancellationToken`** consistently — all async methods accept and pass cancellation tokens.

---

## Reporting Security Issues

If you discover a security vulnerability in this library, please report it responsibly by emailing the repository owner rather than opening a public issue. See the [GitHub Security Policy](https://docs.github.com/en/code-security/getting-started/adding-a-security-policy-to-your-repository) for guidance.
