# Blazor Text2Image Demo

This .NET 8 Blazor Web App is a documentation and component gallery for
`ElBruno.Text2Image.BlazorComponents`. It demonstrates all five components
without requiring cloud credentials or downloading a local model.

## Run

```bash
cd src/samples/BlazorText2ImageDemo
dotnet run
```

Open the URL printed by the app. Bootstrap 5.3 is loaded from jsDelivr by
`Components/App.razor`.

## Pages

| Route | Component | Demonstrates |
|---|---|---|
| `/prompt-editor` | `PromptEditor` | Prompt, negative prompt, options, and `GenerationRequest` callback |
| `/gallery` | `GeneratedImageGallery` | Responsive image grid, metadata, modal preview, and download |
| `/progress` | `InferenceProgressBar` | Indeterminate progress and optional ETA |
| `/backend` | `BackendSelector` | `ExecutionProvider` selection and availability badges |
| `/caption` | `ImageCaptionViewer` | Optional image/caption content and optional re-caption callback |

The sample registers the scoped state service with
`AddText2ImageBlazorComponents()`. Its pages use placeholder data and callback
output so they remain safe to run on a clean machine.

## Connecting real generation

Install a backend package and register the native
`ElBruno.Text2Image.IImageGenerator` implementation in your application. A
host service can handle `PromptEditor`'s `GenerationRequest`, call
`GenerateAsync`, then pass the resulting `ImageGenerationResult` list to
`GeneratedImageGallery`. Report local inference state through
`Text2ImageState.SetProgress`.

The component library does not create a generator, probe hardware, or provide
captioning. `ImageCaptionViewer` accepts an optional caption and exposes
`OnRecaption` for an application-owned caption service. See
[`docs/blazor-components.md`](../../../docs/blazor-components.md) for the
complete API reference.
