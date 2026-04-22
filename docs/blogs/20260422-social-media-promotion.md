# Social Media Promotion — GPT-Image & Skills Announcement

This document provides assets for promoting the GPT-Image-1.5/2.0 and CLI skill integration announcement on LinkedIn and Twitter/X.

---

## Image Assets

### Hero Image (1:1 Square for LinkedIn/Twitter)

**Prompt for MAI-Image-2e or FLUX.2 (1280×1280):**

```
A vibrant, modern developer's workspace split into three zones of equal importance:
LEFT ZONE: IDE window with the ElBruno.Text2Image CLI command prompt visible, showing t2i command suggestions. Blue accent lighting.
CENTER ZONE: Three colored streams of light converging toward a central glowing nexus point: one RED stream (GPT-Image-2), one GREEN stream (FLUX.2), one GOLD stream (MAI-Image-2). The convergence symbolizes unified access.
RIGHT ZONE: A puzzle piece icon overlaid with "skill" text, representing the GitHub Copilot and Claude Code integration. Purple accent lighting.
CONNECTING ELEMENT: The three streams flow into the puzzle piece, which then channels into the CLI terminal shown on the left.
STYLE: Modern, tech-forward, clean geometric composition with subtle gradients. Professional but approachable. High contrast against a dark background (charcoal #1a1a1a). No text overlays — pure visual metaphor.
DIMENSIONS: 1280×1280 pixels (1:1 square).
```

**Command to generate (Windows PowerShell):**

```powershell
t2i "A vibrant, modern developer's workspace split into three zones of equal importance: LEFT ZONE: IDE window with the ElBruno.Text2Image CLI command prompt visible, showing t2i command suggestions. Blue accent lighting. CENTER ZONE: Three colored streams of light converging toward a central glowing nexus point: one RED stream (GPT-Image-2), one GREEN stream (FLUX.2), one GOLD stream (MAI-Image-2). The convergence symbolizes unified access. RIGHT ZONE: A puzzle piece icon overlaid with 'skill' text, representing the GitHub Copilot and Claude Code integration. Purple accent lighting. CONNECTING ELEMENT: The three streams flow into the puzzle piece, which then channels into the CLI terminal shown on the left. STYLE: Modern, tech-forward, clean geometric composition with subtle gradients. Professional but approachable. High contrast against a dark background (charcoal #1a1a1a). No text overlays — pure visual metaphor. DIMENSIONS: 1280×1280 pixels (1:1 square)." --provider foundry-mai2 --width 1280 --height 1280 --output 20260422-gpt-images-and-skills-social-square.png
```

**Output:** `20260422-gpt-images-and-skills-social-square.png` (to be generated and stored in this folder)

---

## LinkedIn Post

### Format: Single Image + Caption

**Copy (Catchy & Professional):**

```
🎨 THREE MODELS. ONE API. ZERO FRICTION.

Introducing unified multi-model image generation for .NET developers.

ElBruno.Text2Image now brings GPT-Image-1.5, GPT-Image-2, FLUX.2 Pro/Flex, and MAI-Image-2 to the same clean interface. No vendor lock-in. No learning curve. Just pick the model that fits your workflow.

🔴 Need reliable, widely available? → GPT-Image-2
🟢 Building photorealistic masterpieces? → FLUX.2 Pro
🟡 Optimizing for speed & balance? → MAI-Image-2
🟣 Perfect text rendering? → FLUX.2 Flex

But here's the kicker: it's also now a SKILL for GitHub Copilot and Claude Code. 

Use `t2i init` in your repo and watch AI agents (Copilot, Claude Code) instantly learn to generate images alongside your code. No documentation needed. The agent just knows.

Whether you're building AI-powered tools, automating design workflows, or crafting the next generation of generative applications, ElBruno.Text2Image gives you the flexibility enterprise teams need and indie devs crave.

Check out the blog post in the comments for deep dives on each model, code examples, and best practices.

#dotnet #ai #imagegen #github #claudecode #texttoimage
```

**Character count:** ~650 (fits LinkedIn's 3000-char limit)

---

## Twitter/X Post

### Format: Image + Viral Thread (3 Tweets)

**Tweet 1 (Lead):**

```
🎨 Meet your new CLI superpower: ElBruno.Text2Image

One API. Four production-grade models. Zero vendor lock-in.

GPT-Image-2 for reliability
FLUX.2 for perfection
MAI-Image-2 for speed

Now also a GitHub Copilot + Claude Code skill.

Thread 🧵
```

**Tweet 2 (Models):**

```
Why four models?

Redundancy + Specialization = Resilience.

If one provider goes down, switch instantly. Photorealistic art? Use FLUX.2. Diagrams with text? FLUX.2 Flex. Fast iteration? MAI-Image-2.

No rewriting code. Same API surface. Pick your tool. Go.
```

**Tweet 3 (Skills):**

```
But wait—it's *also* a GitHub Copilot skill.

`t2i init` in your repo.

Now @GitHub Copilot and @claude_ai understand how to generate images. Agents pick it up. No prompt needed. No documentation.

This is what production AI looks like.

Get started → https://github.com/elbruno/ElBruno.Text2Image
```

**Character counts (each tweet):**
- Tweet 1: ~160 chars
- Tweet 2: ~170 chars  
- Tweet 3: ~195 chars

---

## Key Talking Points (For Repurposing)

Use these in DMs, talks, presentations, or other channels:

1. **"Same API, four models"** — devs don't rewrite code to try different models
2. **"Provider diversity means reliability"** — one down, switch to another
3. **"Skills aren't documentation"** — AI agents learn from the skill, instantly
4. **"Enterprise resilience, indie dev freedom"** — no lock-in, maximum flexibility
5. **"Photorealistic or text-perfect"** — FLUX.2 handles both use cases
6. **"CLI + Copilot integration"** — bring text-to-image into your AI workflows

---

## Posting Timeline (Suggested)

| Day | Action | Channel |
|-----|--------|---------|
| **Day 1** | Publish blog post | GitHub + Twitter thread + LinkedIn post (all same day) |
| **Day 2** | Retweet/quote high-engagement tweets | Twitter (amplify thread) |
| **Day 3** | Respond to comments, engage community | All channels |
| **Day 5** | Follow-up tweet: "Here's how to use it" (code snippet) | Twitter |
| **Day 7** | LinkedIn recap: "This week, we announced..." (recap benefits) | LinkedIn |

---

## File Checklist

- [ ] `20260422-gpt-images-and-skills-social-square.png` — generated & saved
- [ ] Social media copy saved (this doc)
- [ ] Blog post live: `20260422-gpt-images-and-skills.md`
- [ ] Hero image (16:9 landscape): `20260422-gpt-images-and-skills-hero.png`

---

## Notes

- **Image generation timing:** Generate the square 1:1 image using t2i, move to this folder before posting
- **Alt text for accessibility:** "Modern developer workspace with three colored light streams converging toward a central nexus, representing unified model access. IDE terminal visible on left, puzzle piece skill icon on right."
- **Best posting times:** LinkedIn 8-10 AM, Twitter 9-11 AM (timezone: UTC-4 or use your local time)
