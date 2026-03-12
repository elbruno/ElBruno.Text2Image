# Decisions

> Shared decision log for the ElBruno.Text2Image team. All agents read this before starting work.

<!-- Scribe merges decisions from .squad/decisions/inbox/ into this file. Do not edit directly — use the inbox. -->

## Decision: Multi-Platform NPU Support

**Date:** 2025-03-12  
**Author:** Kaylee (Core Dev)  
**Status:** Implemented

### Context

Added support for Neural Processing Units (NPUs) from Qualcomm (Snapdragon X) and Intel (Core Ultra) to enable hardware-accelerated inference on modern ARM and x86 devices with dedicated AI accelerators.

### Decision

Created two new runtime packages following the established thin-wrapper pattern:

1. **ElBruno.Text2Image.Npu.Qualcomm**
   - Uses Microsoft.ML.OnnxRuntime.QNN (v1.24.3)
   - Targets Qualcomm Snapdragon X Hexagon Tensor Processor (HTP)
   - Requires QNN backend path configuration: `QnnHtp.dll`

2. **ElBruno.Text2Image.Npu.Intel**
   - Uses Intel.ML.OnnxRuntime.OpenVino (v1.21.0)
   - Targets Intel Core Ultra AI Boost NPU
   - Requires OpenVINO runtime on the system

### Implementation Details

- **Auto-detection order**: CUDA → DirectML → QNN → OpenVINO → CPU
- **ExecutionProvider enum**: Added QualcommQnn = 3, IntelOpenVino = 4
- **Detection logic**: QNN checks for "QNNExecutionProvider" string; OpenVINO attempts AppendExecutionProvider_OpenVINO("NPU") and checks for exceptions
- **Graceful fallback**: Both providers fall back to CPU if hardware unavailable

### Rationale

- Consistent with existing GPU wrapper architecture (no source code duplication)
- NPU support enables lower power consumption and better thermals for inference
- Automatic detection ensures users don't need to configure execution providers manually
- Thin wrappers allow users to install only the packages they need

### Impact

- Users with Qualcomm or Intel NPU hardware can now get accelerated inference
- No breaking changes to existing API surface
- Solution file updated to include both new projects
