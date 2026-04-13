# 🖼️ MAI-Image-2 Just Dropped — And .NET Support Is Already Here

![MAI-Image-2 generated sample](../mai_image2_output.png)

⚠️ _This blog post was created with the help of AI tools. Yes, I used a bit of magic from language models to organize my thoughts and automate the boring parts, but the geeky fun and the 🖼️ in C# are 100% mine._

Hi!

When Microsoft announced [MAI-Image-2](https://microsoft.ai/news/introducing-MAI-Image-2/), I immediately thought: _"I need to add this to ElBruno.Text2Image. Today."_

So I did. 😄

**MAI-Image-2** is Microsoft's new image generation model on Azure AI Foundry — high-quality generation, a **synchronous API** (no polling!), a 32K character prompt limit, and flexible dimensions. And it's already supported in **ElBruno.Text2Image** with the same clean interface you already know.

Let me show you how it works.

---

## ☁️ Getting Started — MAI-Image-2 on Azure AI Foundry

MAI-Image-2 delivers high-quality image generation with a simpler developer experience than FLUX.2. The API is synchronous — you send a request, you get an image back. No 202 status codes, no polling loops, no waiting callbacks. Just a prompt and a picture.

Here's all you need:

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

using var generator = new MaiImage2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key",
    modelId: "MAI-Image-2");

var result = await generator.GenerateAsync(
    "a simple flat icon of a paintbrush and a sparkle, purple and blue gradient, white background",
    new ImageGenerationOptions { Width = 1024, Height = 1024 });

await result.SaveAsync("mai-image2-output.png");
Console.WriteLine($"Generated in {result.InferenceTimeMs}ms");
```

That image at the top of this post? **Generated with MAI-Image-2 using this exact library.** 🎉

### Setting up credentials

The library reads from User Secrets, environment variables, or `appsettings.json`. For local development:

```bash
dotnet user-secrets set MAI_IMAGE2_ENDPOINT "https://your-resource.services.ai.azure.com"
dotnet user-secrets set MAI_IMAGE2_API_KEY "your-api-key-here"
dotnet user-secrets set MAI_IMAGE2_MODEL_ID "MAI-Image-2"
```

> 💡 **Fun fact:** MAI-Image-2 uses a dedicated `/mai/v1/images/generations` endpoint. The library handles this automatically — just provide your `.services.ai.azure.com` base URL and it builds the correct API path for you.

---

## ⚡ Key Differences from FLUX.2

If you're already using FLUX.2 with this library, here's how MAI-Image-2 compares:

| Feature | MAI-Image-2 | FLUX.2 |
|---|---|---|
| **API style** | Synchronous (direct response) | Asynchronous (202 + polling) |
| **API path** | `/mai/v1/images/generations` | BFL provider path |
| **Prompt limit** | 32,000 characters | ~1,000 characters |
| **Min dimensions** | 768px (per side) | 256px |
| **Max dimensions** | 1M total pixels | Model-dependent |
| **Interface** | `IImageGenerator` | `IImageGenerator` |
| **DI support** | ✅ Same pattern | ✅ Same pattern |
| **Endpoint auto-conversion** | ✅ | ✅ |

The synchronous API is a big deal for developer experience. No more writing polling loops or handling intermediate states. Send a prompt, get an image. Done.

---

## 🔌 Same Interface, Multiple Backends

This is the part I love most. Every generator — cloud or local — implements the same `IImageGenerator` interface. Swap backends without changing your application logic:

```csharp
// MAI-Image-2 (cloud)
IImageGenerator generator = new MaiImage2Generator(endpoint, apiKey, modelId: "MAI-Image-2");

// FLUX.2 Pro (cloud)
IImageGenerator generator = new Flux2Generator(endpoint, apiKey, modelId: "FLUX.2-pro");

// Stable Diffusion 1.5 (local)
IImageGenerator generator = new StableDiffusion15();

// Same API for all three
var result = await generator.GenerateAsync("a beautiful landscape");
```

One interface. Three completely different backends. Your app doesn't care which one is running.

---

## 💉 Dependency Injection

If you're building with DI, the library has an extension method ready to go:

```csharp
services.AddMaiImage2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key",
    modelId: "MAI-Image-2");
```

Same pattern as the FLUX.2 registration. Inject `IImageGenerator` and you're done.

---

## 🔗 Links

- **Repository:** [github.com/elbruno/ElBruno.Text2Image](https://github.com/elbruno/ElBruno.Text2Image)
- **NuGet:** [nuget.org/packages/ElBruno.Text2Image.Foundry](https://www.nuget.org/packages/ElBruno.Text2Image.Foundry)
- **MAI-Image-2 Announcement:** [Introducing MAI-Image-2](https://microsoft.ai/news/introducing-MAI-Image-2/)
- **Setup Guide:** [MAI-Image-2 Setup Guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/mai-image-2-setup-guide.md)
- **v0.8.0 Release:** [github.com/elbruno/ElBruno.Text2Image/releases/tag/v0.8.0](https://github.com/elbruno/ElBruno.Text2Image/releases/tag/v0.8.0)

Happy coding! 🚀

_El Bruno_
