# Wash — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Learnings

*Append new learnings below this line.*

- **BFL API Content-Length requirement (2025-07-25):** The Black Forest Labs API on Azure Foundry requires an explicit `Content-Length` header. `JsonContent.Create()` may use chunked transfer encoding, which omits it. Use `JsonSerializer.SerializeToUtf8Bytes()` + `ByteArrayContent` instead.
- **Source-generated JSON context:** `Flux2JsonContext` is the source-generated `JsonSerializerContext` for Foundry request/response types. Always use it for serialization (e.g., `Flux2JsonContext.Default.Flux2Request`).
- **Key file:** `src/ElBruno.Text2Image.Foundry/Flux2Generator.cs` — FLUX.2 cloud API client (BFL Native API via Azure Foundry). Handles both sync (200) and async (202 + polling) patterns.
- **Build/test commands:** `dotnet build --no-restore` and `dotnet test --no-build` — multi-target (net8.0 + net10.0), 87 tests per TFM.
