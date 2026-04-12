# Decision: Use ByteArrayContent for BFL API requests

**Date:** 2025-07-25
**Author:** Wash (Backend Dev)
**Status:** Implemented
**Issue:** #5

## Context

The BFL (Black Forest Labs) API on Azure Foundry began requiring an explicit `Content-Length` header on all requests (reported since April 12, 2026). The previous implementation used `JsonContent.Create()`, which can use chunked transfer encoding and omit `Content-Length`.

## Decision

Serialize the JSON request body to UTF-8 bytes using the source-generated `Flux2JsonContext`, then construct a `ByteArrayContent` with `application/json; charset=utf-8` content type. `ByteArrayContent` always sets `Content-Length` from the byte array length.

## Implications

- Any future HTTP POST/PUT calls to the BFL API should follow this pattern (serialize to bytes first, then `ByteArrayContent`).
- This is safer in general for APIs that reject chunked transfer encoding.
- No public API surface was changed — this is an internal implementation detail.
