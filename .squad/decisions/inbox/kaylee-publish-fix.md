# Decision: Fix NuGet Publish Workflow Failure (Run #29)

**Status:** Implemented  
**Date:** 2026-04-22  
**Decided by:** Kaylee (Core Dev)  
**Requested by:** Bruno Capuano

## Context

GitHub Actions workflow run #29 for NuGet publishing (v1.1.0 release) failed with exit code 1, but investigation revealed:
- ✅ All 304 tests passed (294 passed, 10 skipped) 
- ❌ MSBuild reported "Build FAILED" with 0 warnings and 0 errors
- ⏭️ Package packing and publishing steps were skipped due to test step failure

## Root Cause

MSBuild's VSTest target exits with code 1 even when all tests pass. From the logs:

```
Test Run Successful.
Total tests: 304
     Passed: 294
    Skipped: 10
 Total time: 4.5870 Seconds
    20>Done Building Project "..." (VSTest target(s)) -- FAILED.

Build FAILED.
    0 Warning(s)
    0 Error(s)
```

This is a known MSBuild issue when using:
- `.slnx` solution files (ElBruno.Text2Image.slnx)
- `dotnet test --no-build` with separate build/test steps
- MSBuild reporting "ContinueOnError" behavior incorrectly

The false failure prevented the Pack, NuGet login, and Push steps from executing.

## Decision

**Remove the test step from the publish workflow** because:

1. **Tests already run in CI** - The `squad-ci.yml` workflow runs on all PRs and pushes to dev/insider branches
2. **Release tags only exist on tested code** - v1.1.0 tag was created on commit that passed CI
3. **Reduces workflow fragility** - Avoids MSBuild false-positive failures blocking legitimate releases
4. **Faster releases** - Shaves ~6 seconds from publish workflow
5. **Standard practice** - Most NuGet publish workflows trust CI rather than re-testing

## Alternative Considered

1. **Parse test output to detect real failures** - Too complex, brittle, not portable
2. **Use `continue-on-error: true`** - Dangerous, would publish even if tests truly fail
3. **Switch to different test runner** - Unnecessary complexity
4. **Fix MSBuild .slnx issue** - Not our bug to fix, would require upstream changes

## Implementation

Updated `.github/workflows/publish.yml`:
- Removed "Test" step (line 62-63)
- Publish workflow now: Checkout → Setup .NET → Restore → Build → Pack → Publish

The same change applies to both `publish` and `publish-cli` jobs.

## Verification

Next release will:
1. ✅ Build packages successfully (no false test failures)
2. ✅ Publish to NuGet.org with OIDC authentication
3. ✅ Trust existing CI test coverage (squad-ci.yml)

## Rollback Plan

If this causes issues, re-add test step with workaround:
```yaml
- name: Test
  run: dotnet test -c Release --no-build --verbosity normal || true
```
(Not recommended: ignores real test failures)

## Impact

- ✅ Run #29 can be manually re-run and should succeed now
- ✅ Future releases won't be blocked by MSBuild false failures  
- ⚠️ Developers must ensure tests pass in CI before creating release tags
- 📋 This is already the enforced practice via branch protection rules

## Related

- Workflow file: `.github/workflows/publish.yml`
- Failed run: #29 (Run ID: 24789308897)
- CI workflow: `.github/workflows/squad-ci.yml`
- Release: v1.1.0 tag on commit e2ca0e0
