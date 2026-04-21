# Release Notes: GPT-Image-1.5 Support

**Versions:**
- **ElBruno.Text2Image.Foundry**: v0.10.0
- **ElBruno.Text2Image.Cli**: v0.11.0

## New Feature: GPT-Image-1.5 (Azure OpenAI DALL-E 3) Support

Complete integration of GPT-Image-1.5 (DALL-E 3) via Azure OpenAI Service into both the Foundry library and CLI tool.

### Library (Foundry v0.10.0)

#### New Class: `GptImage1p5Generator`
- Generates images using Azure OpenAI DALL-E 3
- Supports fixed sizes: 1024×1024, 1792×1024, 1024×1792
- Automatic size mapping for unsupported dimensions
- Full XML documentation and health check validation
- Integrates with `IImageGenerator` interface

#### Dependencies
- **Azure.AI.OpenAI** (v2.1.x) — New dependency for Azure OpenAI SDK

#### Features
- DPAPI secret storage on Windows
- Health check validation for Azure connectivity
- Clear error messages for auth and deployment failures
- `EnsureModelAvailableAsync()` for pre-flight validation

### CLI Tool (v0.11.0)

#### New Adapter: `FoundryGptImage1p5Adapter`
- Enables `t2i generate --provider foundry-gpt1p5 "your prompt"`
- Configurable deployment name and endpoint
- Environment variable support: `GPT_IMAGE_1P5_ENDPOINT`, `GPT_IMAGE_1P5_API_KEY`, `GPT_IMAGE_1P5_MODEL`
- Integrated into CLI's provider discovery and configuration

#### Setup Guide
- Complete Azure Portal walkthrough for setting up GPT-Image-1.5 deployment
- Configuration examples for both CLI and library code
- Troubleshooting section for common issues

### Sample Project
- **scenario-15-gpt-image-1p5-cloud** — Full working example
  - Demonstrates credentials via environment variables
  - Shows size mapping behavior
  - Includes usage patterns

### Testing
- 6 skippable integration tests for GPT-Image-1.5
- Unit tests for size mapping and error handling
- Environment variable auto-detection for test execution
- All existing tests continue to pass (486+ tests)

### Documentation
- Comprehensive setup guide: `docs/gpt-image-1p5-setup-guide.md`
- Updated README with new provider option
- Sample project includes inline documentation
- Troubleshooting guide for common deployment issues

### Breaking Changes
- **None** — Fully backward compatible
  - Existing configurations continue to work
  - New feature is opt-in via provider selection

### Build Status
✅ All 486+ tests passing  
✅ Zero warnings (fixed XML documentation)  
✅ Both .nupkg and .snupkg symbol packages included  
✅ Azure.AI.OpenAI dependency verified in manifests

## Installation

### Library
```bash
dotnet add package ElBruno.Text2Image.Foundry --version 0.10.0
```

### CLI Tool
```bash
dotnet tool install --global ElBruno.Text2Image.Cli --version 0.11.0
```

Or update existing installation:
```bash
dotnet tool update --global ElBruno.Text2Image.Cli
```

## Getting Started

### CLI Example
```bash
# Configure Azure OpenAI credentials
export GPT_IMAGE_1P5_ENDPOINT="https://<your-resource>.openai.azure.com/"
export GPT_IMAGE_1P5_API_KEY="your-api-key"
export GPT_IMAGE_1P5_MODEL="gpt-image-1.5"

# Generate an image
t2i generate --provider foundry-gpt1p5 "A serene landscape with mountains"
```

### Library Example
```csharp
var generator = new GptImage1p5Generator(
    endpoint: "https://<your-resource>.openai.azure.com/",
    apiKey: "your-api-key"
);

var result = await generator.GenerateAsync(
    "A serene landscape with mountains",
    new ImageGenerationOptions { Width = 1024, Height = 1024 }
);

File.WriteAllBytes("output.png", result.ImageBytes);
```

## Commit
- Commit: `e447d1a`
- Branch: `feature/gpt-image-1.5-support`

## Next Steps
1. Create git tags for releases
2. Publish packages to NuGet.org
3. Create GitHub release with these notes
4. Announce in project documentation
