# Timeout Configuration Testing — Jayne

**Date:** 2026-04-22  
**Context:** Parallel test development for timeout feature (#19) while River implements

## Decision: Test Configuration Over Timeout Enforcement

**Problem:** How to test timeout functionality given:
1. Azure SDK (GPT generators) manages its own HTTP layer — can't mock like Flux2/MAI-2
2. FakeHttpHandler with Thread.Sleep doesn't reliably trigger HttpClient.Timeout (thread scheduling dependent)
3. Need comprehensive coverage without flaky timeout enforcement tests

**Decision:** Focus tests on timeout **configuration correctness**, not timeout **enforcement behavior**.

**Test categories implemented:**
- ✅ Constructor accepts timeout parameter (all 4 generators)
- ✅ HttpClient.Timeout property is set correctly
- ✅ Null timeout preserves existing HttpClient.Timeout
- ✅ Boundary values (1s, 24h, infinite) accepted
- ✅ Invalid values (negative, zero) rejected
- ✅ Multiple generators with different timeouts don't interfere
- ✅ Backward compatibility (no timeout param = unchanged behavior)
- ⚠️ Actual timeout enforcement under delay — not reliably testable with FakeHttpHandler

**Rationale:**
1. **Azure SDK isolation** — GPT generators use Azure.AI.OpenAI SDK. Testing HttpClient.Timeout property change verifies timeout will be honored by SDK without mocking Azure internals.
2. **Flake avoidance** — Thread.Sleep timeout tests are flaky (scheduler dependent). Better to verify configuration than chase race conditions.
3. **Configuration correctness** — If timeout is set correctly on HttpClient, the SDK/HttpClient will enforce it in production. Trust the platform.
4. **Test value** — Constructor parameter handling, null handling, boundary validation, generator independence are all valuable without simulating network delays.

**Test count:** 31 tests
- 6 HttpClient.Timeout API tests
- 20 generator-specific constructor tests (5 per generator × 4 generators)
- 5 cross-cutting tests (independence, backward compat, edge cases)

**Implications:**
- Future constructor parameter testing should follow same pattern: verify configuration, not full behavior simulation
- Timeout enforcement is tested via integration/smoke tests against real APIs (not unit tests)
- Pattern applies to other Azure SDK-wrapped features (retry policies, request headers, etc.)

**Files:**
- `TimeoutConfigurationTests.cs` — comprehensive timeout config tests

**Coverage:** 31 tests, 100% passing on net10.0

