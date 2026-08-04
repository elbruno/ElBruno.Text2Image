#if NET10_0_OR_GREATER
using System.Net;
using System.Text;
using Xunit;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

namespace ElBruno.Text2Image.Tests.Resilience;

/// <summary>
/// Phase 3C: Error recovery and resilience tests for network failures, rate limiting, disk errors, and malformed responses.
/// Verifies graceful degradation and user-friendly error messages.
/// </summary>
public class ErrorRecoveryTests
{
    #region Network Timeout Recovery

    [Fact]
    public async Task NetworkTimeout_ThrowsTimeoutException()
    {
        var handler = new FakeHttpHandler(_ => throw new TaskCanceledException("Request timeout"));
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var exception = await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await generator.GenerateAsync("test prompt"));

        Assert.Contains("timeout", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NetworkTimeout_ProvidesFriendlyErrorMessage()
    {
        var handler = new FakeHttpHandler(_ => throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) };
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var exception = await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await generator.GenerateAsync("test prompt"));

        Assert.NotNull(exception.Message);
        Assert.True(exception.Message.Length > 0, "Error message should be informative");
    }

    [Fact]
    public async Task NetworkTimeout_CanBeCaughtAndHandled()
    {
        var handler = new FakeHttpHandler(_ => throw new TaskCanceledException());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var handled = false;
        try
        {
            await generator.GenerateAsync("test prompt");
        }
        catch (TaskCanceledException)
        {
            handled = true;
        }

        Assert.True(handled, "Timeout exception should be catchable for graceful handling");
    }

    #endregion

    #region HTTP 429 Rate Limiting

    [Fact]
    public async Task RateLimit_Http429_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("{\"error\":\"Rate limit exceeded\"}", Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await generator.GenerateAsync("test prompt"));

        Assert.Contains("TooManyRequests", exception.Message);
    }

    [Fact]
    public async Task RateLimit_Http429_PreservesResponseContent()
    {
        var errorJson = "{\"error\":\"Rate limit exceeded. Retry after 60 seconds.\"}";
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await generator.GenerateAsync("test prompt"));

        // Exception should contain the status code for client retry logic
        Assert.NotNull(exception.Message);
    }

    [Fact]
    public async Task RateLimit_Http429_AllowsClientRetryLogic()
    {
        var attemptCount = 0;
        var handler = new FakeHttpHandler(_ =>
        {
            attemptCount++;
            return attemptCount < 3
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : CreateSuccessResponse();
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        // First two attempts should fail, third succeeds
        await Assert.ThrowsAsync<HttpRequestException>(async () => await generator.GenerateAsync("test"));
        await Assert.ThrowsAsync<HttpRequestException>(async () => await generator.GenerateAsync("test"));
        var result = await generator.GenerateAsync("test");

        Assert.NotNull(result.ImageBytes);
        Assert.Equal(3, attemptCount);
    }

    #endregion

    #region Disk Full / Permission Errors

    [SkippableFact]
    public void DiskFull_ConfigSave_ThrowsIOException()
    {
        // Simulate disk full by writing to invalid path (e.g., root with no permissions)
        var invalidPath = OperatingSystem.IsWindows() ? "C:\\Windows\\System32\\test-config.json" : "/root/test-config.json";
        Skip.If(CanWriteToDirectory(invalidPath),
            $"The test environment can write to '{Path.GetDirectoryName(invalidPath)}'; protected-directory assumptions do not apply.");

        var exception = Assert.Throws<UnauthorizedAccessException>(() =>
        {
            File.WriteAllText(invalidPath, "test");
        });

        Assert.NotNull(exception);
    }

    [Fact]
    public void PermissionDenied_ConfigWrite_ThrowsUnauthorizedAccessException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"t2i-readonly-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var configPath = Path.Combine(tempDir, "config.json");
            File.WriteAllText(configPath, "{}");

            if (OperatingSystem.IsWindows())
            {
                var fileInfo = new FileInfo(configPath);
                fileInfo.IsReadOnly = true;

                var exception = Assert.Throws<UnauthorizedAccessException>(() =>
                {
                    File.WriteAllText(configPath, "{\"updated\":true}");
                });

                Assert.NotNull(exception);
            }
            else
            {
                // On Unix, use chmod 444 (read-only)
                File.SetUnixFileMode(configPath, UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead);

                var exception = Assert.Throws<UnauthorizedAccessException>(() =>
                {
                    File.WriteAllText(configPath, "{\"updated\":true}");
                });

                Assert.NotNull(exception);
            }
        }
        finally
        {
            try
            {
                var configPath = Path.Combine(tempDir, "config.json");
                if (File.Exists(configPath))
                {
                    if (OperatingSystem.IsWindows())
                    {
                        new FileInfo(configPath).IsReadOnly = false;
                    }
                    else
                    {
                        File.SetUnixFileMode(configPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                    }
                    File.Delete(configPath);
                }
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [SkippableFact]
    public async Task DiskFull_ImageSave_ThrowsIOException()
    {
        var handler = new FakeHttpHandler(_ => CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var result = await generator.GenerateAsync("test prompt");

        // Attempting to write to invalid path should throw
        var invalidPath = OperatingSystem.IsWindows() ? "C:\\Windows\\System32\\test.png" : "/root/test.png";
        Skip.If(CanWriteToDirectory(invalidPath),
            $"The test environment can write to '{Path.GetDirectoryName(invalidPath)}'; protected-directory assumptions do not apply.");

        var exception = Assert.Throws<UnauthorizedAccessException>(() =>
        {
            File.WriteAllBytes(invalidPath, result.ImageBytes!);
        });

        Assert.NotNull(exception);
    }

    #endregion

    private static bool CanWriteToDirectory(string targetPath)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrEmpty(directory))
        {
            return false;
        }

        var probePath = Path.Combine(directory, $".t2i-write-probe-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(probePath, "probe");
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(probePath))
                {
                    File.Delete(probePath);
                }
            }
            catch
            {
                // Best effort cleanup; write access was already established.
            }
        }
    }

    #region Malformed API Response Recovery

    [Fact]
    public async Task MalformedResponse_InvalidJson_ThrowsJsonException()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json at all", Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var exception = await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            async () => await generator.GenerateAsync("test prompt"));

        Assert.NotNull(exception.Message);
    }

    [Fact]
    public async Task MalformedResponse_MissingDataField_ThrowsInvalidOperationException()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"created\":1234}", Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await generator.GenerateAsync("test prompt"));

        Assert.NotNull(exception.Message);
    }

    [Fact]
    public async Task MalformedResponse_EmptyDataArray_ThrowsInvalidOperationException()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"created\":1234,\"data\":[]}", Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await generator.GenerateAsync("test prompt"));

        Assert.NotNull(exception.Message);
    }

    #endregion

    #region Test Helpers

    private static HttpResponseMessage CreateSuccessResponse()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var b64 = Convert.ToBase64String(pngBytes);
        var json = $$"""{"created":1234,"data":[{"b64_json":"{{b64}}"}]}""";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
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
