# Scenario 17: Side-by-side model comparison grid

Prompt used for all models:

> a futuristic city at sunset, digital art

| Model | Inference time (ms) | Steps used | Model size | Output |
|---|---:|---:|---|---|
| StableDiffusion15 | 219240 | 20 | ~4 GB | `sd15.png` |
| LcmDreamshaperV7 | _(generated at runtime)_ | 4 | ~4 GB | `lcm.png` |
| SdxlTurbo | _(generated at runtime)_ | 4 | ~8 GB | `sdxl-turbo.png` |

## Validation run result in this PR environment

Command used:

```bash
dotnet ./bin/Debug/net10.0/scenario-17-model-comparison-grid.dll
```

Result:

- `StableDiffusion15` image was generated and saved as `sd15.png`.
- The run then failed on `LcmDreamshaperV7` because `unet/model.onnx_data` was missing in the local cache.
- `SdxlTurbo` was not executed because the app stopped at the LCM failure.

Generated output:

![StableDiffusion15 output](./sd15.png)

Error excerpt:

```text
Microsoft.ML.OnnxRuntime.OnnxRuntimeException: [ErrorCode:RuntimeException]
Exception during initialization: filesystem error: cannot get file size:
No such file or directory [.../lcm-dreamshaper-v7-onnx/unet/model.onnx_data]
```
