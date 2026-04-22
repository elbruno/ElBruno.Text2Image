# Session: Environment Variable Security Review

**Date:** 2026-04-20  
**Agent:** Mal  
**Duration:** Security analysis session

## Summary

Mal completed security review of CLI Secrets/ backends. Key finding: environment variables are insecure for local development (process tree, shell history, dotfile leakage). Recommended blog post reorder to prioritize OS-native encrypted storage (DPAPI/file) for local dev, limit env vars to CI/CD with explicit warnings. No code changes needed—CLI already implements secure defaults. Recommendation documented for Bruno's approval.

## Decision

Write decision to `.squad/decisions/inbox/mal-secret-storage-recommendation.md` for merging into decisions.md.
