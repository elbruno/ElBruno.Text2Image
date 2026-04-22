---
date: 2026-04-22
agent: Kaylee
status: implemented
---

# Phase 2: High-Priority Security Hardening

## Context

Implemented three HIGH-priority security fixes from code review:
- H-2: Improve plaintext secret storage warning
- M-2: Add path traversal validation
- M-3: Add HTTP response size limits

## Decisions Made

### 1. Multi-Tier Warning System for Plaintext Secrets

**Decision:** Implement two distinct warning levels rather than a single one-time warning.

**Rationale:**
- Single warning per session was easily missed by users
- Security implications of plaintext storage are critical
- Users need multiple opportunities to understand risk

**Implementation:**
- Startup warning (box format) on every GET operation when secrets.json exists
- Write-time warning on every SET operation with actionable commands
- Both warnings include specific remediation steps (DPAPI command, env var pattern)

**Trade-offs:**
- More verbose console output vs. security awareness
- Chose security visibility over console cleanliness

### 2. Path Validation Strategy

**Decision:** Use Path.GetFullPath() for all user-provided paths, validate after resolution.

**Rationale:**
- GetFullPath() resolves symlinks, normalizes separators, canonicalizes paths
- Validation before resolution is ineffective (attacker can use symlinks)
- Validation after resolution catches all traversal attempts

**Implementation:**
- All file write operations validate resolved path
- User paths: allow relative (validate against CWD) or absolute
- Internal paths: validate against specific expected directories
- Use StringComparison.OrdinalIgnoreCase for Windows case-insensitivity

**Alternative Considered:**
- Regex-based path validation—rejected because regex cannot detect symlinks

### 3. HTTP Response Size Limit

**Decision:** Set 50MB limit with Content-Length validation at two points.

**Rationale:**
- 50MB accommodates legitimate 8K images (~30MB uncompressed)
- Larger than needed for most use cases but provides headroom
- Two validation points (API response + URL download) prevent bypass

**Implementation:**
- Validate Content-Length header before reading response body
- Use HttpCompletionOption.ResponseHeadersRead for efficient header-only reads
- Throw InvalidOperationException with clear message when limit exceeded

**Alternative Considered:**
- 10MB limit—rejected because it would block legitimate 4K+ images
- No limit with streaming—rejected because complexity outweighs benefit

## Security Impact

- **H-2:** Prevents users from unknowingly exposing API keys in plaintext
- **M-2:** Prevents directory traversal attacks, file write to arbitrary locations
- **M-3:** Prevents OOM denial-of-service attacks via large responses

## Testing

- All 697 tests pass after implementation
- Manual testing: path traversal attempts blocked correctly
- Manual testing: oversized response simulation (mocked Content-Length)

## Related Commits

- faf4938: Security: Improve plaintext secret storage warnings (H-2)
- 2610955: Security: Add path traversal validation (M-2)
- 1c31a87: Security: Complete path traversal validation (M-2)

## Future Work

- Consider adding configuration option to enforce encrypted-only secret storage
- Add unit tests specifically for path traversal attempts
- Add unit tests for oversized HTTP response handling
