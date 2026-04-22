# Skill: Model Usage Patterns

**Confidence:** `high`  
**Last Updated:** 2026-04-21  
**Domain:** Multi-model text-to-image generation, cloud APIs, credential management

## Quick Reference

**Supported Models (v0.15.0):**
- Local: Stable Diffusion 1.5, 2.1, SDXL Turbo, LCM Dreamshaper
- Cloud: FLUX.2 (Foundry), MAI-Image-2 (Foundry), GPT-Image-1.5 (Azure OpenAI), GPT-Image-2 (Azure OpenAI)

---

## Model Selection Criteria

Use this matrix to recommend the right model for a task:

| Criteria | Best Choice | Why |
|----------|------------|-----|
| **High-quality photorealistic** | FLUX.2 Pro (Foundry) | Best-in-class quality |
| **Text-heavy design, UI mockups** | FLUX.2 Flex (Foundry) | Optimized for rendered text |
| **High-quality general purpose** | MAI-Image-2 (Foundry) | Solid quality, lower latency |
| **DALL-E style (editorial, professional)** | GPT-Image-1.5 (Azure OpenAI) | Consistent with editorial workflows |
| **Cutting-edge generation** | GPT-Image-2 (Azure OpenAI) | Next-gen model (new capability) |
| **Fast, no cloud dependency** | Local (SD 1.5 or SDXL Turbo) | ~5-10s inference on GPU |
| **CPU-only, no GPU** | Stable Diffusion 1.5 | Works anywhere, ~2-3min inference |

---

## Setup Pattern Checklist

Every cloud model follows this pattern. When adding a new model or troubleshooting:

### 1. **Endpoint & Deployment**
- [ ] Verify endpoint URL is correct (Azure Foundry: `https://{resource}.services.ai.azure.com`)
- [ ] Verify deployment/model name matches Azure console
- [ ] Test connectivity: `curl -I https://{endpoint}`

### 2. **Authentication**
- [ ] API key generated and accessible
- [ ] Not committed to source code (use user-secrets or env vars)
- [ ] Has correct permissions (image generation scope)

### 3. **Implementation (C# Library)**
- [ ] Add NuGet package: `ElBruno.Text2Image.Foundry`
- [ ] Create generator instance with correct parameters:
  ```csharp
  new Flux2Generator(endpoint, apiKey, modelName, modelId)
  new MaiImage2Generator(endpoint, apiKey, modelName, modelId)
  new GptImage1p5Generator(endpoint, apiKey, deploymentName)
  new GptImage2Generator(endpoint, apiKey, deploymentName)
  ```
- [ ] Call `GenerateAsync(prompt, options)` with optional ImageGenerationOptions

### 4. **CLI Tool Setup**
- [ ] Install: `dotnet tool install --global ElBruno.Text2Image.Cli`
- [ ] Run `t2i config` for interactive setup
- [ ] Or set environment variables: `T2I_{PROVIDER}_{FIELD}` (e.g., `T2I_FOUNDRY_FLUX2_ENDPOINT`)
- [ ] Verify: `t2i config show` (masks API key)

### 5. **User Secrets (Development)**
```bash
dotnet user-secrets set "T2IModels:Flux2:Endpoint" "https://..."
dotnet user-secrets set "T2IModels:Flux2:ApiKey" "..."
```

---

## Known Quirks & Workarounds

### **BFL FLUX.2 API — Content-Length Header**
**Problem:** Plain JsonContent.Create() fails silently on Azure Foundry.  
**Solution:** Use ByteArrayContent with explicit Content-Length header.  
**Location:** `Flux2Generator.cs` lines 200-207  
**Why:** BFL API on Azure requires explicit Content-Length; JsonContent doesn't set it.

```csharp
var json = JsonSerializer.Serialize(request, Flux2JsonContext.Default.Flux2Request);
var content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
content.Headers.ContentType = new("application/json");
// Content-Length is automatically set
```

### **Azure OpenAI — OIDC Policy Binding**
**Problem:** NuGet trusted-publisher OIDC token exchange fails with "Resource not accessible by integration."  
**Solution:** All NuGet push jobs MUST live in `.github/workflows/publish.yml` (not separate workflows).  
**Why:** OIDC policy is bound to workflow filename; new jobs in different files fail auth.  
**Location:** `.github/workflows/publish.yml`

### **YAML Backslash Continuations on windows-latest**
**Problem:** Backslash line continuations break on pwsh (default shell for windows-latest).  
**Solution:** Add `shell: bash` to steps that use backslash continuations.  
**Why:** PowerShell treats trailing `\` as a literal argument, causing MSB1008 errors.

### **Source-Gen JSON in Flux2Generator**
**Pattern:** Flux2Generator uses source-gen JsonSerializerContext (Flux2JsonContext).  
**Benefit:** Eliminates reflection overhead; Flux2Request properties auto-serialized.  
**Note:** No manual context updates needed when adding new Flux2Request fields.

---

## Credential Resolution Hierarchy (CLI & Library)

When the CLI or library needs a credential, it checks in this order (first match wins):

1. **CLI Flags** — `--endpoint`, `--api-key`, `--deployment-name`
2. **Environment Variables** — `T2I_{PROVIDER}_{FIELD}` (e.g., `T2I_FOUNDRY_FLUX2_ENDPOINT`)
3. **User Secrets** (development) — `T2IModels:{Provider}:{Field}` (DPAPI on Windows)
4. **Config File** (local) — `~/.t2i/config.json`
5. **Fail** — If none found, error with helpful message

**Example:** To use GPT-Image-1.5:
```bash
# Option 1: CLI flags (highest priority)
t2i --provider azure-openai-gpt-image-15 --endpoint https://... --api-key ... "prompt"

# Option 2: Environment variables
export T2I_AZURE_OPENAI_GPT_IMAGE_15_ENDPOINT="https://..."
export T2I_AZURE_OPENAI_GPT_IMAGE_15_APIKEY="..."
t2i "prompt"

# Option 3: User secrets (development)
dotnet user-secrets set "T2IModels:AzureOpenAiGptImage15:Endpoint" "https://..."
t2i "prompt"
```

---

## Model-Specific Implementation Notes

### **FLUX.2 (Foundry)**
- **Generator:** `Flux2Generator`
- **Variants:** `FLUX.2-pro` (default), `FLUX.2-flex`
- **HTTP Method:** POST with ByteArrayContent (see workaround above)
- **Size:** Up to 1536×1536
- **Key File:** `src/ElBruno.Text2Image.Foundry/Flux2Generator.cs`

### **MAI-Image-2 (Foundry)**
- **Generator:** `MaiImage2Generator`
- **Variants:** `MAI-Image-2` (default), `MAI-Image-2e`
- **Size:** Min 768px, max 1M total pixels
- **Response:** Synchronous (no polling)
- **Key File:** `src/ElBruno.Text2Image.Foundry/MaiImage2Generator.cs`

### **GPT-Image-1.5 (Azure OpenAI)**
- **Generator:** `GptImage1p5Generator`
- **Size:** 1024×1024, 1792×1024, 1024×1792
- **Uses:** Azure.AI.OpenAI SDK (official)
- **Key File:** `src/ElBruno.Text2Image.Foundry/GptImage1p5Generator.cs`
- **Setup:** Requires Azure OpenAI deployment of `gpt-image-15`

### **GPT-Image-2 (Azure OpenAI)**
- **Generator:** `GptImage2Generator`
- **Size:** Configurable via options
- **Uses:** Azure.AI.OpenAI SDK (official)
- **Key File:** `src/ElBruno.Text2Image.Foundry/GptImage2Generator.cs`
- **Status:** Newest model, recommended for high-fidelity generation

---

## Common Issues & Solutions

| Issue | Cause | Fix |
|-------|-------|-----|
| "Unauthorized (401)" | Wrong API key or endpoint | Verify credentials via `t2i config show` |
| "MSB1008: Only one project can be specified" | Backslash continuation on Windows | Add `shell: bash` to CI step |
| "Token exchange failed (401)" | NuGet OIDC in wrong workflow file | Move job to `.github/workflows/publish.yml` |
| "Silent failure on Foundry" | Missing Content-Length header | Use ByteArrayContent (Flux2 pattern) |
| "Image doesn't match prompt" | Wrong model selected | Check model variant or try FLUX.2 Flex for text |
| "Rate limiting" | Too many requests | Implement exponential backoff; see Flux2Generator retry logic |

---

## When to Use This Skill

- **Adding a new model provider:** Reference setup pattern and credential hierarchy
- **Debugging auth/endpoint issues:** Check credential resolution hierarchy
- **Implementing HTTP client code:** Review FLUX.2 workaround for Content-Length
- **Setting up CI/CD for release:** Review Azure OIDC policy binding quirk
- **Choosing a model for a feature:** Use model selection matrix

---

## References

- **User Guides:** `docs/gpt-image-1p5-setup-guide.md`, `docs/flux2-setup-guide.md`, `docs/mai-image-2-setup-guide.md`
- **CLI Reference:** `docs/cli-tool.md`
- **Architecture:** `docs/architecture.md`
- **Security:** `docs/security.md` (credential management)
- **Release Notes:** `GITHUB_RELEASE_v0.15.0.md`
