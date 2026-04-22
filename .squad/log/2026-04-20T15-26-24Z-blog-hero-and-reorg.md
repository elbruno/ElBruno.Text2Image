# Session Log — 2026-04-20T15:26:24Z

**Topic:** Blog post hero image + docs/blogs reorganization

## Who Worked
- River (general-purpose, sync, claude-sonnet-4.5)
- Coordinator (lightweight in-session, diagnostics + generation)
- Kaylee (general-purpose, sync, claude-sonnet-4.5)

## What Was Done
1. **Hero image generation (River):**
   - Drafted blog post hero image prompt
   - Attempted generation via t2i CLI
   - Issue discovered: Azure deployment name mismatch (`MAI-Image-2` vs actual `MAI-Image-2e`)

2. **Azure diagnostics & fix (Coordinator):**
   - Diagnosed deployment name mismatch via `az cognitiveservices account deployment list`
   - Reconfigured t2i with correct deployment name
   - Generated final hero image: `images/20260420-introducing-t2i-cli-hero.png` (1280x768)
   - Created companion prompt doc: `docs/blogs/20260420-introducing-t2i-cli-image-prompt.md`

3. **Documentation reorganization (Kaylee):**
   - Moved 4 blog docs to new `docs/blogs/` directory
   - Updated 8 relative links across 4 files
   - Updated 1 CHANGELOG reference

## Decisions Made
1. Repo blog images: `images/{YYYYMMDD}-{slug}.png` + companion `docs/blogs/{YYYYMMDD}-{slug}-image-prompt.md`
2. All blog posts live in `docs/blogs/` (separate from technical reference docs)
3. t2i is canonical tool for repo image assets (dogfooding the tool)
4. Azure MAI deployment at `bruno-agents-04-resource` is named `MAI-Image-2e` (not `MAI-Image-2`)

## Outcomes
✅ Complete. Files staged for Bruno's review:
- `images/20260420-introducing-t2i-cli-hero.png`
- `docs/blogs/20260420-introducing-t2i-cli-image-prompt.md`
- `docs/blogs/20260420-introducing-t2i-cli.md` (with hero + link)
- `docs/blogs/blog-post-text2image-dotnet.md`
- `docs/blogs/blog-post-mai-image-2.md`
- `CHANGELOG.md` (updated)

Not committed — awaiting Bruno's review.
