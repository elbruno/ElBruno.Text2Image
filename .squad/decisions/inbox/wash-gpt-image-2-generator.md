# Decision: GptImage2Generator Implementation

**Author:** Wash (Backend Dev)  
**Date:** 2026-04-20  
**Status:** Implemented

## Context

Bruno Capuano requested implementation of the GptImage2Generator class for the Foundry library to support the GPT-Image-2 model (Azure OpenAI DALL-E 3 v2). The CLI adapter `FoundryGptImage2Adapter` already existed but was missing the underlying generator implementation, causing build failures.

## Decision

Created `src/ElBruno.Text2Image.Foundry/GptImage2Generator.cs` following the established pattern from `GptImage1p5Generator`:

- **Class:** Sealed, implements both `IImageGenerator` and `Microsoft.Extensions.AI.IImageGenerator`
- **API Pattern:** Uses Azure.AI.OpenAI.ImageClient with Azure OpenAI Service endpoint
- **Deployment:** Default "gpt-image-2" (configurable)
- **Model Name:** Default "GPT-Image-2" (configurable)
- **Supported Sizes:** 1024×1024, 1024×1536, 1536×1024 (same as 1.5)
- **Prompt Limit:** 4000 characters maximum
- **Error Handling:** Validates HTTPS endpoints, throws on null/empty prompt, handles aspect ratio fallback
- **M.E.AI Integration:** Implements explicit interface for Microsoft.Extensions.AI with property passthrough

## Implementation Details

1. **Constructor:** Requires endpoint (HTTPS), apiKey, optional modelName/deploymentName/httpClient
2. **HttpClient Management:** Owns and disposes HttpClient if not injected (5-minute timeout)
3. **Size Mapping:** MapToSizeString() handles aspect ratio fallback (>1.2 → landscape, <0.85 → portrait)
4. **Async Pattern:** Single-shot GenerateImageAsync() call (no polling needed for Azure OpenAI)
5. **Result:** Returns ImageGenerationResult with bytes, metadata, inference time

## Integration Points

- **CLI:** `FoundryGptImage2Adapter` in `ElBruno.Text2Image.Cli/Providers/` consumes the generator
- **DI Registration:** Already registered in `ProviderServiceCollectionExtensions.cs`
- **Config:** Supports configurable endpoint/model via ConfigStore (backward compat with secret resolver)

## Testing

- **Build:** Succeeded with 0 warnings, 0 errors
- **Pattern Consistency:** Matches GptImage1p5Generator implementation exactly
- **API Compatibility:** Both IImageGenerator interfaces implemented

## Implications

- Consumers can now use GPT-Image-2 via CLI (`t2i generate --provider foundry-gpt-image-2`)
- Library users can instantiate GptImage2Generator directly
- Future Azure OpenAI image models should follow this pattern (sealed class, ImageClient API, size mapping)
- No breaking changes to existing APIs

## Files Changed

- **NEW:** `src/ElBruno.Text2Image.Foundry/GptImage2Generator.cs`
- **EXISTING:** `src/ElBruno.Text2Image.Cli/Providers/FoundryGptImage2Adapter.cs` (already referenced the class)
- **EXISTING:** `src/ElBruno.Text2Image.Cli/Infrastructure/ProviderServiceCollectionExtensions.cs` (already registered)
