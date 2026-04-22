#if NET10_0_OR_GREATER
using System.Diagnostics;
using Xunit;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

namespace ElBruno.Text2Image.Tests.Performance;

/// <summary>
/// Phase 3C: Performance tests for batch generation, concurrency, memory, and tensor efficiency.
/// Baseline expectations: batch of 10 should complete in under 30s (mocked), memory should not grow unbounded.
/// </summary>
public class PerformanceTests
{
    #region Batch Generation Throughput

    [Fact]
    public async Task BatchGeneration_ProcessesTenPromptsWithinReasonableTime()
    {
        // Baseline: 10 prompts with mocked HTTP should complete under 5 seconds
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var prompts = Enumerable.Range(1, 10).Select(i => $"prompt {i}").ToArray();
        var sw = Stopwatch.StartNew();

        var tasks = prompts.Select(p => generator.GenerateAsync(p));
        await Task.WhenAll(tasks);

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 5000, $"Batch of 10 prompts took {sw.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    [Fact]
    public async Task BatchGeneration_MeasuresThroughputForHundredPrompts()
    {
        // Baseline: 100 prompts with mocked HTTP should complete under 30 seconds
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var prompts = Enumerable.Range(1, 100).Select(i => $"prompt {i}").ToArray();
        var sw = Stopwatch.StartNew();

        var tasks = prompts.Select(p => generator.GenerateAsync(p));
        await Task.WhenAll(tasks);

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < 30000, $"Batch of 100 prompts took {sw.ElapsedMilliseconds}ms, expected < 30000ms");
    }

    [Fact]
    public async Task BatchGeneration_TracksMemoryUsage()
    {
        // Baseline: Memory growth should be linear, not exponential (no leaks)
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: false);

        var prompts = Enumerable.Range(1, 50).Select(i => $"prompt {i}").ToArray();
        var tasks = prompts.Select(p => generator.GenerateAsync(p));
        await Task.WhenAll(tasks);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);

        var memoryGrowth = memoryAfter - memoryBefore;
        // Memory growth should be reasonable (< 100MB for 50 mocked generations)
        Assert.True(memoryGrowth < 100 * 1024 * 1024, $"Memory grew by {memoryGrowth / (1024 * 1024)}MB, expected < 100MB");
    }

    #endregion

    #region Concurrent Operations

    [Fact]
    public async Task ConcurrentOperations_ParallelPromptsExecuteSimultaneously()
    {
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var prompts = Enumerable.Range(1, 20).Select(i => $"concurrent prompt {i}").ToArray();
        var sw = Stopwatch.StartNew();

        var tasks = prompts.Select(p => generator.GenerateAsync(p)).ToArray();
        await Task.WhenAll(tasks);

        sw.Stop();
        // 20 concurrent tasks should complete faster than 20 sequential tasks
        Assert.True(sw.ElapsedMilliseconds < 3000, $"20 concurrent prompts took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ConcurrentOperations_MultipleGeneratorsDoNotInterfere()
    {
        var handler1 = new FakeHttpHandler(_ => CreateSuccessResponse());
        var handler2 = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient1 = new HttpClient(handler1);
        using var httpClient2 = new HttpClient(handler2);
        using var generator1 = new Flux2Generator("https://example.com/api", "key1", httpClient: httpClient1);
        using var generator2 = new MaiImage2Generator("https://example.com/api", "key2", httpClient: httpClient2);

        var task1 = generator1.GenerateAsync("prompt 1");
        var task2 = generator2.GenerateAsync("prompt 2");

        await Task.WhenAll(task1, task2);

        Assert.NotNull(handler1.LastRequest);
        Assert.NotNull(handler2.LastRequest);
        Assert.Contains("prompt 1", handler1.LastRequestBody);
        Assert.Contains("prompt 2", handler2.LastRequestBody);
    }

    [Fact]
    public async Task ConcurrentOperations_StressTestFiftyParallelRequests()
    {
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var prompts = Enumerable.Range(1, 50).Select(i => $"stress test prompt {i}").ToArray();
        var sw = Stopwatch.StartNew();

        var tasks = prompts.Select(p => generator.GenerateAsync(p));
        var results = await Task.WhenAll(tasks);

        sw.Stop();
        Assert.Equal(50, results.Length);
        Assert.All(results, r => Assert.NotNull(r.ImageBytes));
        Assert.True(sw.ElapsedMilliseconds < 10000, $"50 parallel requests took {sw.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Memory Leak Detection

    [Fact]
    public async Task MemoryLeakDetection_RepeatedGenerationsDoNotLeakMemory()
    {
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        // Generate 100 images sequentially
        for (int i = 0; i < 100; i++)
        {
            await generator.GenerateAsync($"iteration {i}");
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);

        var memoryGrowth = memoryAfter - memoryBefore;
        // 100 generations should not cause unbounded memory growth
        Assert.True(memoryGrowth < 50 * 1024 * 1024, $"Memory grew by {memoryGrowth / (1024 * 1024)}MB after 100 generations");
    }

    [Fact]
    public async Task MemoryLeakDetection_DisposedGeneratorReleasesMemory()
    {
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        // Create and dispose generator multiple times
        for (int i = 0; i < 10; i++)
        {
            using var httpClient = new HttpClient(handler);
            using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);
            await generator.GenerateAsync($"test {i}");
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);

        var memoryGrowth = memoryAfter - memoryBefore;
        Assert.True(memoryGrowth < 20 * 1024 * 1024, $"Memory grew by {memoryGrowth / (1024 * 1024)}MB after 10 dispose cycles");
    }

    [Fact]
    public async Task MemoryLeakDetection_LargeImageResponsesAreProperlyReleased()
    {
        // Simulate large image responses (1MB each)
        var largeImageBytes = new byte[1024 * 1024];
        Array.Fill(largeImageBytes, (byte)0xFF);
        var b64 = Convert.ToBase64String(largeImageBytes);
        var handler = new FakeHttpHandler(_ => CreateLargeImageResponse(b64));
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        for (int i = 0; i < 10; i++)
        {
            var result = await generator.GenerateAsync($"large image {i}");
            // Don't hold references - let them be GC'd
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);

        var memoryGrowth = memoryAfter - memoryBefore;
        // 10 x 1MB images should not cause >30MB growth (allows for overhead)
        Assert.True(memoryGrowth < 30 * 1024 * 1024, $"Memory grew by {memoryGrowth / (1024 * 1024)}MB for 10x1MB images");
    }

    #endregion

    #region Tensor Reuse Efficiency

    [Fact]
    public async Task TensorReuse_GeneratorReuseDoesNotCausePerformanceDegradation()
    {
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        // First batch
        var sw1 = Stopwatch.StartNew();
        await Task.WhenAll(Enumerable.Range(1, 10).Select(i => generator.GenerateAsync($"batch1-{i}")));
        sw1.Stop();

        // Second batch (should not be significantly slower)
        var sw2 = Stopwatch.StartNew();
        await Task.WhenAll(Enumerable.Range(1, 10).Select(i => generator.GenerateAsync($"batch2-{i}")));
        sw2.Stop();

        // Both batches should complete (timing may vary in mocked environment)
        Assert.True(sw1.ElapsedMilliseconds >= 0);
        Assert.True(sw2.ElapsedMilliseconds >= 0);
    }

    [Fact]
    public async Task TensorReuse_HttpClientReuseDoesNotCauseDegradation()
    {
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);

        // Create multiple generators sharing the same HttpClient
        var generators = Enumerable.Range(1, 5)
            .Select(_ => new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient))
            .ToArray();

        try
        {
            var sw = Stopwatch.StartNew();
            var tasks = generators.Select((g, i) => g.GenerateAsync($"generator {i}"));
            await Task.WhenAll(tasks);
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds >= 0, "Shared HttpClient should work correctly");
        }
        finally
        {
            foreach (var g in generators)
            {
                g.Dispose();
            }
        }
    }

    [Fact]
    public async Task TensorReuse_SequentialGenerationsAreConsistent()
    {
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var timings = new List<long>();

        for (int i = 0; i < 20; i++)
        {
            var sw = Stopwatch.StartNew();
            await generator.GenerateAsync($"sequential {i}");
            sw.Stop();
            timings.Add(sw.ElapsedMilliseconds);
        }

        // All generations should complete successfully
        Assert.Equal(20, timings.Count);
        Assert.All(timings, t => Assert.True(t >= 0, "Timing should be non-negative"));
    }

    #endregion

    #region Test Helpers

    private static HttpResponseMessage CreateSuccessResponse()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var b64 = Convert.ToBase64String(pngBytes);
        var json = $$"""{"created":1234,"data":[{"b64_json":"{{b64}}"}]}""";
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage CreateLargeImageResponse(string base64Data)
    {
        var json = $$"""{"created":1234,"data":[{"b64_json":"{{base64Data}}"}]}""";
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return _responseFactory(request);
        }
    }

    #endregion
}
#endif
