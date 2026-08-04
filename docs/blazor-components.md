# Blazor Components

`ElBruno.Text2Image.BlazorComponents` is a .NET 8 Razor Class Library with
Bootstrap-friendly building blocks for image-generation experiences. The
package supplies UI and state helpers; it does not select or create an image
backend for you.

## Install

```bash
dotnet add package ElBruno.Text2Image.BlazorComponents
```

Add one generation package as well, for example `ElBruno.Text2Image.Cpu`,
`ElBruno.Text2Image.Cuda`, `ElBruno.Text2Image.DirectML`, or
`ElBruno.Text2Image.Foundry`.

Register the component state service in `Program.cs`:

```csharp
builder.Services.AddText2ImageBlazorComponents();
```

The components use Bootstrap class names. Add Bootstrap to the host
application (or provide equivalent CSS) when you want the default layout and
responsive gallery styling.

## Components

### `PromptEditor`

Provides positive and negative prompt fields, guidance and step sliders, and
an optional seed. It raises `EventCallback<GenerationRequest>`:

```razor
<PromptEditor OnGenerate="HandleGenerate"
              DefaultGuidanceScale="7.5"
              DefaultSteps="20" />
```

`GenerationRequest` contains `Prompt`, `ImageGenerationOptions Options`, and
`string? NegativePrompt`. The negative prompt is passed to the callback; the
component does not send a request or invoke a generator itself.

### `GeneratedImageGallery`

Displays `IReadOnlyList<ImageGenerationResult>` as a responsive grid. Cards
can show prompt, seed, inference duration, and model metadata. Selecting an
image opens a modal, and each image has a browser download link.

```razor
<GeneratedImageGallery Images="images"
                       Columns="3"
                       ShowMetadata="true" />
```

### `InferenceProgressBar`

Reads progress from the scoped `Text2ImageState` service:

```razor
<InferenceProgressBar ShowEta="true" />
```

Application code reports progress with
`state.SetProgress(new InferenceProgress(current, total, eta))`. If current
or total is missing (or total is zero), the component renders an indeterminate
bar. ETA is shown only when `ShowEta` is true and an estimated duration is
available.

### `BackendSelector`

Provides an `ExecutionProvider` radio group (`Auto`, `Cpu`, `Cuda`, and
`DirectML`) and stores the selected value in `Text2ImageState`.

```razor
<BackendSelector AvailableProviders="availableProviders"
                 OnBackendChanged="HandleBackendChanged"
                 ShowAvailabilityBadge="true" />
```

When `AvailableProviders` is omitted, only `Auto` and `Cpu` are marked
available. The component does not probe hardware or configure a cloud
provider; supply the providers your application supports.

### `ImageCaptionViewer`

Displays an optional image URL and optional caption:

```razor
<ImageCaptionViewer ImageUrl="@imageUrl"
                    Caption="@caption"
                    OnRecaption="HandleRecaption" />
```

`OnRecaption` is optional. The Re-Caption button is rendered only when a
callback is supplied, and the callback receives the current image URL.
`AutoCaption` is an optional parameter for host integrations; the component
does not implement a captioning service or automatically obtain captions.

## Using the native generator API

The package works with the library's native `ElBruno.Text2Image.IImageGenerator`
interface:

```csharp
public sealed class GenerationService(IImageGenerator generator)
{
    public Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default) =>
        generator.GenerateAsync(prompt, options, cancellationToken);
}
```

`ImageGenerationOptions` supports model directory, `ExecutionProvider`,
inference steps, guidance scale, dimensions, seed, and reference images.
Generation returns `ImageGenerationResult`, which includes PNG bytes, model
name, prompt, seed, dimensions, and inference time.

The components do not require an `IImageGenerator` registration. Register the
concrete generator (or your own adapter) in the host application and pass it
to the service that handles `GenerationRequest`.

## Sample

Run the component gallery:

```bash
cd src/samples/BlazorText2ImageDemo
dotnet run
```

Open the displayed URL and use the navigation to inspect all five components.
The sample demonstrates callbacks and state updates; it intentionally does not
require cloud credentials or download a local model. See the sample
[README](../src/samples/BlazorText2ImageDemo/README.md) for page-by-page
details.
