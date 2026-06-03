# Scenario 18 — MAI-Image-2.5 Cloud API (Microsoft Foundry)

Generates an image using **MAI-Image-2.5** (or **MAI-Image-2.5-Flash**) via the Microsoft
Foundry OpenAI-compatible images API. No local ONNX models are required.

## Models

| Model | Focus | Model ID |
|-------|-------|----------|
| MAI-Image-2.5 | Highest fidelity & creative detail | `MAI-Image-2.5` |
| MAI-Image-2.5-Flash | Speed-optimized / low latency | `MAI-Image-2.5-Flash` |

Both variants are served by the same `MaiImage25Generator` class — select the model with
the `modelId` parameter.

## Configuration

Set the endpoint and API key from your Microsoft Foundry deployment using user secrets:

```bash
dotnet user-secrets set MAI_IMAGE25_ENDPOINT "https://your-resource.services.ai.azure.com"
dotnet user-secrets set MAI_IMAGE25_API_KEY "your-api-key-here"
dotnet user-secrets set MAI_IMAGE25_MODEL_ID "MAI-Image-2.5"   # or MAI-Image-2.5-Flash
```

Environment variables (`MAI_IMAGE25_ENDPOINT`, `MAI_IMAGE25_API_KEY`, `MAI_IMAGE25_MODEL_ID`)
and `appsettings.json` are also supported.

## Run

```bash
dotnet run --framework net10.0
```

The generated image is saved as `mai_image25_output.png` in the working directory.

See [docs/mai-image-2.5-setup-guide.md](../../../docs/mai-image-2.5-setup-guide.md) for full setup details.
