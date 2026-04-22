# 🖼️ Hero Image — "GPT-Image Models + t2i Skill" Announcement

Companion document for the blog post announcing GPT-Image-1.5 and GPT-Image-2 model support plus t2i skill integration for AI coding agents (GitHub Copilot, Claude Code). This records the exact prompt, CLI invocation, and model settings used to generate the hero image — so the result is reproducible and the prompt can be tweaked for future variants.

**Image file:** [`../../images/20260422-gpt-images-and-skills-hero.png`](../../images/20260422-gpt-images-and-skills-hero.png)

---

## 🎯 Prompt

```text
A cinematic wide tech illustration: a modern developer's IDE window occupies the
left third, dark mode with soft cyan and purple glow, beside it floats an AI
agent chat interface with minimal geometric bot icon. From the chat interface,
three luminous streams of energy branch outward and upward — one golden, one
electric blue, one vibrant magenta — each stream carries flowing thumbnails of
different AI-generated images (landscapes, portraits, abstract art) representing
multiple model pathways. At the convergence point, a glowing puzzle piece icon
symbolizes skill integration. Deep space blue gradient background with subtle
circuit board patterns and floating geometric shapes. Isometric perspective,
clean composition, modern tech aesthetic with holographic accents, premium
digital render, 16:9 framing. No readable text, no logos, no watermarks.
```

## ⚙️ Generation Command

Generated via `t2i` CLI (note: foundry-flux2 would be preferred for quality, but foundry-mai2 is used here for speed):

```powershell
t2i "A cinematic wide tech illustration: a modern developer's IDE window occupies the left third, dark mode with soft cyan and purple glow, beside it floats an AI agent chat interface with minimal geometric bot icon. From the chat interface, three luminous streams of energy branch outward and upward — one golden, one electric blue, one vibrant magenta — each stream carries flowing thumbnails of different AI-generated images (landscapes, portraits, abstract art) representing multiple model pathways. At the convergence point, a glowing puzzle piece icon symbolizes skill integration. Deep space blue gradient background with subtle circuit board patterns and floating geometric shapes. Isometric perspective, clean composition, modern tech aesthetic with holographic accents, premium digital render, 16:9 framing. No readable text, no logos, no watermarks." `
  --provider foundry-mai2 `
  --width 1280 `
  --height 768 `
  --output images/20260422-gpt-images-and-skills-hero.png
```

## 📋 Parameters

| Setting | Value |
|---|---|
| **Provider** | `foundry-mai2` (Microsoft Foundry, fast iteration) |
| **Model / deployment** | `MAI-Image-2e` |
| **Endpoint** | `https://bruno-agents-04-resource.services.ai.azure.com/` |
| **Dimensions** | 1280 × 768 (16:9 landscape, 983,040 px — under the 1,048,576 cap) |
| **Target filename** | `images/20260422-gpt-images-and-skills-hero.png` |
| **Generated for** | GPT-Image-1.5/2.0 + t2i skill announcement (2026-04-22) |

## 🧠 Prompt Design Notes

### Multi-Concept Visual Strategy

This prompt weaves together three core concepts into one cohesive image:

1. **AI Coding Agent Integration** (IDE + agent chat interface)
   - Left third: familiar developer environment anchors the scene in the user's workflow
   - Agent chat interface beside it establishes the conversational AI context
   - Minimal geometric bot icon avoids cliché robot faces, stays modern

2. **Model Choice & Pathways** (three luminous streams, different colors)
   - Three distinct streams = visual metaphor for multiple model options (GPT-Image-1.5, GPT-Image-2, MAI-Image-2, FLUX.2)
   - Different colors (golden, electric blue, magenta) reinforce the "choice" concept — not one pipeline, multiple paths
   - Flowing image thumbnails in each stream show the *output* diversity different models can produce

3. **Skill Integration** (glowing puzzle piece at convergence)
   - Puzzle piece is universal "integration" symbol in tech UX
   - Placed at stream convergence = the skill is the unifying mechanism that makes all model access possible
   - Glowing treatment gives it hero status in the composition

### Technical Constraints

- **No readable text:** Explicit "no readable text, no logos, no watermarks" prevents garbled pseudo-glyphs in IDE chrome or chat bubbles
- **IDE/chat "silhouettes":** Described by color/glow/shape but not expecting pixel-perfect UI renders — the model isn't good at precise UI replication
- **16:9 framing:** Called out in prompt + enforced in CLI args to guide composition; this is a blog hero, not a square icon
- **Isometric perspective:** Adds depth and modern tech-aesthetic feel without forcing orthogonal "screenshot" flatness
- **Circuit board patterns + geometric shapes:** Subtle sci-fi texture keeps it interesting without overpowering the main subjects

### Color & Lighting

- **Deep space blue gradient:** Dark enough to read on both light/dark blog themes, tech-forward without being harsh
- **Cyan + purple glow on IDE:** Standard dev-tool color scheme (VS Code vibes)
- **Three distinct stream colors:** Golden (warmth, premium), electric blue (tech, speed), magenta (creativity, energy) — each carries semantic weight
- **Holographic accents:** Modern tech aesthetic, premium feel, differentiates from flat corporate stock art

### Composition Balance

- IDE and chat occupy left third, leaving two-thirds for the "magic happening" (the streams and integration point)
- Streams flow upward and outward = visual motion, energy, expansion (the capability is growing)
- Puzzle piece convergence at upper-right or center-right = eye flows left-to-right (IDE → agent → streams → integration), natural reading direction for most blog readers

## 🔁 Reproducing or Remixing

1. Ensure `t2i` is configured for `foundry-mai2` (see [`../cli-tool.md`](../cli-tool.md) and [`../mai-image-2-setup-guide.md`](../mai-image-2-setup-guide.md)).
2. Run the command above. Outputs are non-deterministic — MAI-Image-2 will produce a new variation each call. Generate 3-5 candidates and select the one with the clearest composition and best color balance.
3. **Remixing for other announcements:**
   - **More/fewer models?** Change the number of streams and their colors (two streams for binary choice, four for expanded ecosystem).
   - **Different feature?** Swap the puzzle piece icon with another symbol (e.g., lightning bolt for "speed", shield for "security", globe for "global availability").
   - **Art direction shift?** Replace "holographic accents" with "watercolor splashes" (artistic), "low-poly 3D" (playful), or "photorealistic depth of field" (premium).

---

_Convention: for any repo-hosted image asset used in a blog post, ship a companion `docs/blogs/{YYYYMMDD}-{slug}-image-prompt.md` with the prompt, command, model, and parameters._
