# SKILL: Security-Conscious Error Messages

**Domain:** Security, Error Handling  
**Created:** 2025-01-26  
**Author:** Kaylee (Core Dev)

## Pattern

When generating error messages that might expose infrastructure details (endpoints, resource names, network topology), use environment-variable-controlled verbosity to prevent information disclosure in production while maintaining developer debugging capability.

## When to Apply

- Error messages that include resolved endpoint URLs
- Error messages containing Azure resource names or service topology
- Any diagnostic output that could reveal internal infrastructure
- HTTP error responses that expose backend details

## Implementation

### Step 1: Create a helper method for error detail generation

```csharp
/// <summary>
/// Builds error hint for 404 responses.
/// In production (default), provides generic guidance.
/// Set T2I_DETAILED_ERRORS=1 environment variable to include full endpoint URL (for debugging only).
/// </summary>
private string BuildErrorHint()
{
    var detailedErrors = Environment.GetEnvironmentVariable("T2I_DETAILED_ERRORS");
    var includeEndpoint = detailedErrors == "1" || detailedErrors == "true";

    if (includeEndpoint)
    {
        // Debug mode: include full endpoint details for troubleshooting
        return "\n\nHint: The endpoint URL may be incorrect. Service uses API at /path/to/api.\n" +
               $"The resolved endpoint was: {_endpoint}\n" +
               "Ensure you provide either:\n" +
               "  - A base URL (e.g., https://your-resource.services.example.com)\n" +
               "  - A full API URL (e.g., https://your-resource.services.example.com/path/to/api)";
    }

    // Production mode: generic error without exposing infrastructure details
    return "\n\nHint: Failed to connect to image generation service. " +
           "Verify your endpoint configuration is correct. " +
           "Set T2I_DETAILED_ERRORS=1 for more diagnostic information.";
}
```

### Step 2: Use the helper in error handling

```csharp
if (!response.IsSuccessStatusCode)
{
    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
    if (errorBody.Length > MaxErrorBodyLength)
        errorBody = errorBody[..MaxErrorBodyLength] + "... (truncated)";

    var hint = response.StatusCode == System.Net.HttpStatusCode.NotFound
        ? BuildErrorHint()
        : "";

    throw new HttpRequestException(
        $"API returned {response.StatusCode}: {errorBody}{hint}");
}
```

## Environment Variable Convention

**Name:** `T2I_DETAILED_ERRORS`  
**Values:**
- Not set (default): Production mode, minimal disclosure
- `"1"` or `"true"`: Debug mode, full diagnostic details

**Rationale:**
- Follows existing T2I_ prefix convention (see EnvVarSecretStore)
- Binary opt-in model (not gradual levels)
- Safe by default
- No code changes needed for debugging

## Production vs. Debug Messages

### Production Mode (Default)
```
MAI-Image-2 API returned 404: {"error": "Not found"}

Hint: Failed to connect to image generation service. Verify your endpoint configuration is correct. Set T2I_DETAILED_ERRORS=1 for more diagnostic information.
```

### Debug Mode (T2I_DETAILED_ERRORS=1)
```
MAI-Image-2 API returned 404: {"error": "Not found"}

Hint: The endpoint URL may be incorrect. MAI-Image-2 uses the MAI API at /mai/v1/images/generations.
The resolved endpoint was: https://my-resource-eastus.services.ai.azure.com/mai/v1/images/generations
Ensure you provide either:
  - A base URL (e.g., https://your-resource.services.ai.azure.com)
  - A full MAI API URL (e.g., https://your-resource.services.ai.azure.com/mai/v1/images/generations)
```

## What NOT to Do

❌ **Don't expose endpoints unconditionally:**
```csharp
throw new HttpRequestException(
    $"Failed to connect to {_endpoint}");  // SECURITY RISK
```

❌ **Don't use gradual verbosity levels:**
```csharp
var level = Environment.GetEnvironmentVariable("T2I_ERROR_LEVEL");
// Avoid: "minimal", "normal", "verbose", "debug"
// Users won't know which level exposes what
```

❌ **Don't require configuration changes:**
```csharp
if (_options.VerboseErrors)  // Requires code/config change
```

## Related Patterns

See also:
- `T2I_DETAILED_HEALTH_CHECKS` for credential-bearing network calls
- EnvVarSecretStore (T2I_ prefix convention)

## References

- Implementation: src/ElBruno.Text2Image.Foundry/MaiImage2Generator.cs (lines 123-148)
- Implementation: src/ElBruno.Text2Image.Foundry/Flux2Generator.cs (lines 135-160)
- Security fix: Commit aad4f5a (H-3)
