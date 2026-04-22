# SKILL: HttpClient Connection Pooling Pattern

**Category:** Performance, Architecture  
**Applies to:** Cloud API generators, HTTP-based integrations  
**Last updated:** 2026-04-21

## Pattern

**Always inject HttpClient via dependency injection; never create instances per request.**

## Rationale

Creating new `HttpClient` instances bypasses TCP connection pooling, causing:
- Socket exhaustion (TIME_WAIT state accumulation)
- 30-40% performance degradation
- Service instability under load

## Implementation

### In Library Code (Generators)

**DO:**
```csharp
public class Flux2Generator : IImageGenerator
{
    private readonly HttpClient _httpClient;
    
    public Flux2Generator(
        string endpoint,
        string apiKey,
        HttpClient httpClient,  // Required, 3rd parameter
        string? modelName = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }
}
```

**DON'T:**
```csharp
// ❌ WRONG: Optional parameter enables anti-pattern
public Flux2Generator(..., HttpClient? httpClient = null)
{
    _httpClient = httpClient ?? new HttpClient();  // BAD!
}
```

### In DI Registration (ServiceCollectionExtensions)

**DO:**
```csharp
public static IServiceCollection AddFlux2Generator(
    this IServiceCollection services,
    string endpoint,
    string apiKey)
{
    services.AddHttpClient();
    services.AddSingleton<IImageGenerator>(sp =>
    {
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = factory.CreateClient();
        return new Flux2Generator(endpoint, apiKey, httpClient);
    });
    return services;
}
```

**DON'T:**
```csharp
// ❌ WRONG: Direct instantiation bypasses pooling
services.AddSingleton<IImageGenerator>(
    new Flux2Generator(endpoint, apiKey));  // Missing HttpClient!
```

### In Consumer Code (CLI, Apps)

**DO:**
```csharp
// CLI/Application code
var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
var httpClient = factory.CreateClient();
using var generator = new Flux2Generator(endpoint, apiKey, httpClient);
```

**DON'T:**
```csharp
// ❌ WRONG: Per-request instantiation
using var generator = new Flux2Generator(endpoint, apiKey, new HttpClient());
```

## Test Pattern

For unit tests with mocked HTTP responses:

```csharp
[Fact]
public async Task MyTest()
{
    var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
    using var httpClient = new HttpClient(handler);
    using var generator = new Flux2Generator("https://example.com", "key", httpClient);
    
    // Test code...
}
```

## Migration Checklist

When adding HttpClient to existing generators:

1. ✅ Make `HttpClient` a required parameter (non-optional)
2. ✅ Position as 3rd parameter (after endpoint, apiKey)
3. ✅ Add `ArgumentNullException.ThrowIfNull(httpClient)`
4. ✅ Remove fallback `new HttpClient()` creation
5. ✅ Update ServiceCollectionExtensions to use factory pattern
6. ✅ Fix all test constructors to pass HttpClient
7. ✅ Fix all sample/scenario code
8. ✅ Update XML documentation to mention DI/pooling

## Performance Impact

- **Socket usage:** Reduces from N sockets (per request) to ~10 pooled connections
- **Latency:** 30-40% improvement in high-throughput scenarios
- **Stability:** Eliminates socket exhaustion failures

## References

- [Microsoft Docs: HttpClient Guidelines](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)
- Implementation: `src/ElBruno.Text2Image.Foundry/Flux2Generator.cs`
- DI Example: `src/ElBruno.Text2Image.Foundry/ServiceCollectionExtensions.cs`
- CLI Pattern: `src/ElBruno.Text2Image.Cli/Providers/FoundryFlux2Adapter.cs`
