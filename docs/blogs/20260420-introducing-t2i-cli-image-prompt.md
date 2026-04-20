# 🖼️ Hero Image — "Meet t2i" Blog Post

Companion document for [`20260420-introducing-t2i-cli.md`](./20260420-introducing-t2i-cli.md). This records the exact prompt, CLI invocation, and model settings used to generate the hero image — so the result is reproducible and the prompt can be tweaked for future variants.

**Image file:** [`../../images/20260420-introducing-t2i-cli-hero.png`](../../images/20260420-introducing-t2i-cli-hero.png)

---

## 🎯 Prompt

```text
A cinematic wide hero illustration: a modern developer's desk at dusk, a glowing
dark-mode terminal window floating center-frame with soft neon teal and magenta
command-line text silhouettes (no readable letters), from the terminal emerges
a swirling ribbon of vivid AI-generated imagery -- brushstrokes, pixel dust,
tiny landscapes, galaxies, and painterly color splashes flowing outward like
magic. Clean minimalist composition, deep navy background with subtle bokeh,
soft rim lighting, photoreal render blended with digital art, 16:9 framing,
high detail, premium tech aesthetic. No text, no logos, no watermarks.
```

## ⚙️ Generation Command

Generated via `t2i` itself (dogfooding — the CLI produced the blog post's own hero image):

```powershell
t2i "<prompt above>" `
  --provider foundry-mai2 `
  --width 1280 `
  --height 768 `
  --output images/20260420-introducing-t2i-cli-hero.png
```

## 📋 Parameters

| Setting | Value |
|---|---|
| **Provider** | `foundry-mai2` (Microsoft Foundry) |
| **Model / deployment** | `MAI-Image-2e` |
| **Endpoint** | `https://bruno-agents-04-resource.services.ai.azure.com/` |
| **Dimensions** | 1280 × 768 (16:9 landscape, 983,040 px — under the 1,048,576 cap) |
| **Generation time** | ~18.2 s |
| **Generated on** | 2026-04-20 |

## 🧠 Prompt Design Notes

- **No readable text in the image.** Diffusion models still struggle with legible glyphs; explicit "no text, no logos, no watermarks" reduces the chance of garbled pseudo-text in the terminal chrome.
- **"Command-line text silhouettes"** gives the model permission to render terminal-y shapes without committing to real letters.
- **16:9 framing** is called out both in the prompt and in `--width`/`--height` to keep the model's composition aligned with the output canvas and make it a good blog header crop.
- **Two-palette contrast** (deep navy + teal/magenta neon) matches common developer-tool hero aesthetics and keeps the subject readable on both light and dark blog themes.
- **"Swirling ribbon of vivid AI-generated imagery"** visually tells the story of the CLI: you type, images flow out.

## 🔁 Reproducing or Remixing

1. Ensure `t2i` is configured for `foundry-mai2` (see [`cli-tool.md`](../cli-tool.md) and [`mai-image-2-setup-guide.md`](../mai-image-2-setup-guide.md)).
2. Run the command above. Outputs are non-deterministic — MAI-Image-2 will produce a new variation each call. Re-run and keep the best of N.
3. To remix, keep the composition anchors (terminal centerpiece, ribbon of imagery, navy background) and swap the color palette or art direction (e.g., "watercolor" → "low-poly 3D" → "cyberpunk").

---

_Convention: for any repo-hosted image asset used in a blog post, ship a companion `docs/{YYYYMMDD}-{slug}-image-prompt.md` with the prompt, command, model, and parameters._
