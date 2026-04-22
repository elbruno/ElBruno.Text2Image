# Security Fixes: Phase 1 Critical Issues

**Author:** Kaylee (Core Dev)  
**Date:** 2025-01-26  
**Status:** Implemented  
**Branch:** feature/code-review-security-perf

## Context

Implemented two critical security fixes identified in code review:
1. **H-3 (CRITICAL):** Endpoint URL exposure in error messages
2. **H-1 (HIGH):** Health check MITM vulnerability

## Decisions

### H-3: Endpoint URL Exposure in Error Messages

**Problem:** Full resolved endpoint URLs (containing Azure resource names and network topology) were exposed in production error messages in both MaiImage2Generator and Flux2Generator.

**Solution:** Environment-variable-controlled error verbosity
- **Production mode (default):** Generic error messages without infrastructure details
- **Debug mode (T2I_DETAILED_ERRORS=1):** Full diagnostic information including endpoint URLs
- Implemented via `BuildErrorHint()` method in both generators
- Preserves developer debugging capability while protecting production infrastructure

**Files Modified:**
- src/ElBruno.Text2Image.Foundry/MaiImage2Generator.cs
- src/ElBruno.Text2Image.Foundry/Flux2Generator.cs
- src/ElBruno.Text2Image.Foundry/ElBruno.Text2Image.Foundry.csproj (added missing Microsoft.Extensions.Http)

**Commit:** aad4f5a

### H-1: Health Check MITM Vulnerability

**Problem:** Health checks sent API keys in Authorization header over HTTP connections without certificate validation, creating MITM attack vector.

**Solution:** Redesigned health checks with security-first approach
- **Default behavior:** Local configuration validation only (no network calls)
- **Opt-in detailed checks:** T2I_DETAILED_HEALTH_CHECKS=1 enables network connectivity tests
- Health checks now verify endpoint + apiKey presence locally
- Network-based validation requires explicit environment variable opt-in

**Rationale:**
1. Configuration validation provides sufficient health signal for most use cases
2. Network-based checks with credentials should be explicit opt-in
3. Eliminates credential exposure during routine health checks
4. Maintains backward compatibility (providers still report healthy/unhealthy status)
5. Developers can enable detailed checks when needed for troubleshooting

**Files Modified:**
- src/ElBruno.Text2Image.Cli/Providers/FoundryFlux2Adapter.cs
- src/ElBruno.Text2Image.Cli/Providers/FoundryMaiImage2Adapter.cs

**Commit:** a730e3e

## Security Pattern Established

Both fixes follow a consistent security pattern:

1. **Safe by default:** Production mode has minimal information disclosure
2. **Opt-in diagnostics:** Detailed/insecure operations require explicit environment variable
3. **T2I_ prefix convention:** Aligns with existing EnvVarSecretStore naming
4. **Binary opt-in:** Values "1" or "true" enable detailed mode

**Environment Variables:**
- `T2I_DETAILED_ERRORS`: Controls error message verbosity (H-3)
- `T2I_DETAILED_HEALTH_CHECKS`: Controls health check network tests (H-1)

## Implications

1. **User impact:** Minimal. Default behavior is more secure. Error messages are less revealing but still actionable.
2. **Developer impact:** Opt-in environment variables available for debugging without code changes.
3. **Future security features:** Should follow this pattern (safe default + explicit opt-in).
4. **Testing:** Pre-existing test failures unrelated to security fixes. Foundry library builds successfully with changes.

## Build Status

- Foundry library: ✅ Clean build (net8.0 + net10.0)
- CLI project: ✅ Clean build (net10.0)
- Tests: ⚠️ Pre-existing failures in test suite (unrelated to security changes)

## Next Steps

1. Consider documenting security environment variables in README/docs
2. Future: Evaluate other error messages for similar information disclosure
3. Future: Review other health check implementations in other adapters
