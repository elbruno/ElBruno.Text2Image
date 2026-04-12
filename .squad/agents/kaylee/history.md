# Kaylee — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Learnings

*Append new learnings below this line.*

- **img2img feature (2025-07-25):** Added `ReferenceImages` property to `ImageGenerationOptions` and `Flux2Request`. The `Flux2Request` uses source-generated `Flux2JsonContext` so new nullable properties with `[JsonIgnore(WhenWritingNull)]` are automatically handled — no manual context changes needed.
- **Flux2Generator serialization pattern:** Request bodies use `JsonSerializer.SerializeToUtf8Bytes` + `ByteArrayContent` to ensure `Content-Length` is set (BFL API requirement). All request property additions flow through this path automatically.
- **M.E.AI integration:** `Text2ImagePropertyNames` in `Extensions/MeaiIntegration.cs` defines well-known keys for `AdditionalProperties` passthrough. New options should add a constant there and handle it in both `FromMeaiOptions` and the generator's explicit M.E.AI implementation.
- **Key file paths:** `ImageGenerationOptions.cs` (shared options), `Flux2Generator.cs` (cloud API generator + JSON DTOs + `Flux2JsonContext`), `Extensions/MeaiIntegration.cs` (M.E.AI converter + property names).
- **Build command:** `dotnet build ElBruno.Text2Image.slnx --no-restore` — verified clean (0 warnings, 0 errors).
