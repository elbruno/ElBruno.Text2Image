# SKILL: Exponential Backoff Polling Strategy

## Context
When polling async operations (cloud APIs, background jobs, etc.), fixed intervals add unnecessary latency for fast completions while potentially overwhelming slow operations.

## Pattern

### Problem
- Fixed 2-second polling: Fast operations wait unnecessarily, slow operations get hammered
- User experience suffers from artificial latency floor
- Resource waste from either too-frequent or too-sparse polling

### Solution: Adaptive Exponential Backoff

```csharp
// Configuration
private static readonly TimeSpan InitialPollDelay = TimeSpan.FromMilliseconds(500);
private static readonly TimeSpan MaxPollDelay = TimeSpan.FromSeconds(5);
private const double PollBackoffMultiplier = 1.5;

// Implementation
var currentDelay = InitialPollDelay;

for (var attempt = 0; attempt < MaxPollAttempts; attempt++)
{
    await Task.Delay(currentDelay, cancellationToken).ConfigureAwait(false);
    
    // Poll the operation
    var status = await PollOperationAsync();
    
    if (status.IsComplete)
        return status.Result;
    
    // Apply exponential backoff for next attempt
    currentDelay = TimeSpan.FromMilliseconds(
        Math.Min(currentDelay.TotalMilliseconds * PollBackoffMultiplier, 
                 MaxPollDelay.TotalMilliseconds));
}

throw new TimeoutException($"Operation did not complete within {MaxPollAttempts} attempts");
```

## Key Parameters

| Parameter | Typical Value | Purpose |
|-----------|--------------|---------|
| Initial Delay | 500ms | Catch fast completions quickly |
| Max Delay | 5s | Cap worst-case API load |
| Multiplier | 1.5x | Balance between aggressive/conservative |
| Max Attempts | 120 | Total timeout budget |

## Progression Example

With 500ms initial, 1.5x multiplier, 5s cap:
- Attempt 1: 500ms
- Attempt 2: 750ms
- Attempt 3: 1.1s
- Attempt 4: 1.7s
- Attempt 5: 2.5s
- Attempt 6: 3.7s
- Attempt 7+: 5s (capped)

## Performance Impact

| Completion Time | Fixed 2s | Exponential | Improvement |
|-----------------|----------|-------------|-------------|
| Fast (<5s) | ~4-6s wait | ~1-2s wait | 75% faster |
| Medium (10-30s) | ~20-30s | ~12-20s | 30-40% faster |
| Slow (60s+) | Same | Same | No change |

## When to Apply

✅ **Use exponential backoff when:**
- Operation completion time is highly variable (seconds to minutes)
- User is waiting synchronously (CLI, web request)
- Fast completions are common
- You control retry logic

❌ **Don't use when:**
- Fixed SLA/contract requires specific intervals
- Operation times are predictable/uniform
- Background processing (no user waiting)
- External rate limiting is stricter than your backoff

## Variants

### Jittered Backoff (for distributed systems)
```csharp
var jitter = Random.Shared.NextDouble() * 0.1; // ±10%
currentDelay = TimeSpan.FromMilliseconds(
    currentDelay.TotalMilliseconds * PollBackoffMultiplier * (1 + jitter));
```

### Configurable via Environment
```csharp
private static readonly TimeSpan InitialPollDelay = 
    TimeSpan.FromMilliseconds(
        int.Parse(Environment.GetEnvironmentVariable("POLL_INITIAL_MS") ?? "500"));
```

## Related Patterns

- **Circuit Breaker:** Protect downstream services from retry storms
- **Retry with backoff:** Similar but for transient failures vs. polling
- **Adaptive timeout:** Adjust max timeout based on historical completion times

## References

- Implemented in: `src/ElBruno.Text2Image.Foundry/Flux2Generator.cs` (lines 28-30, 373-430)
- Commits: bbf2b7b (2026-04-21)
- Related: Phase 2 Performance Polish

---
**Author:** Wash  
**Created:** 2026-04-21  
**Tags:** performance, polling, backoff, async, latency
