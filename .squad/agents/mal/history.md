# Mal — History

**Project:** ElBruno.Text2Image (.NET AI text-to-image)

**CLI:** Spectre.Console.Cli v0.49.1 (DI). IProviderAdapter. RequiredFields + RequiredSecrets split. Secret chain: CLI > env > DPAPI/plaintext.

**Security:** T2I_DETAILED_ERRORS, T2I_DETAILED_HEALTH_CHECKS (defaults secure).

**GPT-Image-2:** 90% complete. Azure OpenAI pattern. Sizes: 1024×1024/1024×1536/1536×1024.

**Versioning:** All 6 in Directory.Build.props. v0.X.Y primary; cli-v0.X.Y informational.

**Phase 3:** 206 tests, 60-65% coverage. 5 security fixes, 73% latency reduction.
