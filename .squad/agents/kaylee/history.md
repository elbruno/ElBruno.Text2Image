# Kaylee — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Learnings

### Multi-Platform NPU Support (2025-03-12)
- Added Qualcomm QNN and Intel OpenVINO NPU execution providers to support Snapdragon X and Core Ultra NPUs
- NPU wrapper packages follow the same thin-shim pattern as GPU wrappers (CPU, CUDA, DirectML)
- Key pattern: Pure .csproj files with no source code, just project reference + runtime package reference
- Auto-detection order: CUDA → DirectML → QNN → OpenVINO → CPU
- QNN requires backend_path configuration: `{ "backend_path", "QnnHtp.dll" }`
- OpenVINO targets NPU specifically with `AppendExecutionProvider_OpenVINO("NPU")`
- ExecutionProvider enum values: QualcommQnn = 3, IntelOpenVino = 4
- File paths: `ExecutionProvider.cs`, `SessionOptionsHelper.cs`, solution file `ElBruno.Text2Image.slnx`

📌 Team update (2026-03-12T16:19:00Z): Multi-Platform NPU Support completed — Kaylee created QNN and OpenVINO wrappers, ExecutionProvider enum updated (QualcommQnn = 3, IntelOpenVino = 4), SessionOptionsHelper detection/creation implemented. Auto-detection order: CUDA → DirectML → QNN → OpenVINO → CPU. Build: 0 errors, 0 warnings. Tests: 91/91 passed. PR #4 created.

*Append new learnings below this line.*
