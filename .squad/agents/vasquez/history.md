# Vasquez History

## Project Context

- Requested by: Bruno Capuano
- Stack: C#, .NET 8/10, GitHub Actions, NuGet, CLI tooling
- Product: A .NET text-to-image library and CLI for cloud and local generation

## Learnings

- Workflow edits should be reviewed for accidental release or CI regressions.
- Real release automation lives in `.github/workflows/publish.yml` and `.github/workflows/publish-cli.yml`; parking old squad workflows under `.github/workflows-disabled/` does not affect package publishing.
- This repo currently operates on `main` plus feature branches, and the old squad promotion flow is stale enough to reference a missing `package.json`, so branch-model validation is a key review step for workflow cleanup.

## Team Updates (2026-04-24)

- **Workflow cleanup review:** Approved Hicks's migration of 12 squad-specific workflows to `.github/workflows-disabled/`. Verified no regression to `publish.yml` and `publish-cli.yml`.
- **Orchestration log:** `.squad/orchestration-log/2026-04-24T18-47-53Z-vasquez.md`

## Test Infrastructure (2026-06-03)

- **ConfigStore Collection Isolation Convention:** Bishop encoded a canonical pattern for APPDATA-mutating tests. Any test class setting `APPDATA`, `LOCALAPPDATA`, or `XDG_CONFIG_HOME` env vars MUST declare `[Collection("ConfigStore")]` to ensure serial execution. This prevents parallel test runs from corrupting each other's config directory resolution.
  - Collection defined in: `src/ElBruno.Text2Image.Tests/Cli/ConfigStoreTests.cs` line 139
  - Applies to: `ConfigStoreTests`, `PlainFileSecretStoreTests`, `DpapiSecretStoreTests`, and any future config-related tests
  - Also see: EndToEnd test fix debt (3 assertions with incorrect exit code expectations for `t2i doctor` command, tagged for follow-up in issue)

