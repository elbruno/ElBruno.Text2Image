# Decision: img2img ReferenceImages API shape

**Author:** Kaylee (Core Dev)
**Date:** 2025-07-25
**Status:** Implemented

## Context

Issue #6 requested image-to-image support for `Flux2Generator`. The BFL API accepts reference images as URLs, base64 strings, or Data URIs in a `referenceImages` array on the request body.

## Decision

- `ReferenceImages` is a `List<string>?` on both `ImageGenerationOptions` (public) and `Flux2Request` (internal). Nullable + `[JsonIgnore(WhenWritingNull)]` preserves backward compat for text-to-image calls.
- A convenience overload `GenerateAsync(prompt, referenceImagePath, options?, ct)` handles file→base64 Data URI conversion so callers don't need to manually encode.
- M.E.AI consumers pass reference images via `AdditionalProperties["reference_images"]` as `List<string>`. This follows the existing pattern for `num_inference_steps`, `seed`, etc.

## Implications

- **Jayne (Tests):** New overload and property need test coverage. The convenience overload throws `FileNotFoundException` for missing paths.
- **River (AI/ML):** Model-specific limits (FLUX.2-pro: 8 images, FLUX.2-flex: 10) are not enforced client-side — the API returns errors for violations. Consider adding client-side validation later if needed.
- **Mal (Architecture):** The `IImageGenerator` interface was intentionally NOT changed to add a reference-image overload, keeping it simple. The file-path overload is Flux2Generator-specific.
