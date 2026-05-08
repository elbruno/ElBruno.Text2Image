# Scenario 17: Side-by-side model comparison grid

Prompt used for all models:

> a futuristic city at sunset, digital art

| Model | Inference time (ms) | Steps used | Model size | Output |
|---|---:|---:|---|---|
| StableDiffusion15 | _(generated at runtime)_ | 20 | ~4 GB | `sd15.png` |
| LcmDreamshaperV7 | _(generated at runtime)_ | 4 | ~4 GB | `lcm.png` |
| SdxlTurbo | _(generated at runtime)_ | 4 | ~8 GB | `sdxl-turbo.png` |

## Validation run result in this PR environment

Command used:

```bash
dotnet run --framework net10.0
```

Result:

- The run stopped before generation because the sandbox could not reach Hugging Face (`huggingface.co:443`).
- No model files were downloaded, so no images were produced in this environment.

Error excerpt:

```text
System.InvalidOperationException: Failed to download required file 'text_encoder/model.onnx' ...
System.Net.Http.HttpRequestException: Resource temporarily unavailable (huggingface.co:443)
```
