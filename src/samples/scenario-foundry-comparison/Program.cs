using System.Diagnostics;
using System.Drawing;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using ElBruno.Text2Image.Samples.FoundryComparison;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var configuration = new ConfigurationBuilder()
    .SetBasePath(projectDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

var settings = FoundryComparisonSettings.Load(configuration);

var services = new ServiceCollection();
services.AddSingleton(configuration);
services.AddSingleton(settings);
services.AddHttpClient();
services.AddSingleton<FoundryAuthHeaderProvider>();
services.AddSingleton(sp => new FoundryComparisonRunner(
    settings,
    sp.GetRequiredService<IHttpClientFactory>(),
    sp.GetRequiredService<FoundryAuthHeaderProvider>(),
    projectDirectory));

using var provider = services.BuildServiceProvider();
var runner = provider.GetRequiredService<FoundryComparisonRunner>();

var readmeContent = await runner.RunAsync();
Console.WriteLine();
Console.WriteLine(readmeContent);

internal sealed class FoundryComparisonRunner(
    FoundryComparisonSettings settings,
    IHttpClientFactory httpClientFactory,
    FoundryAuthHeaderProvider authHeaderProvider,
    string projectDirectory)
{
    private const string Prompt = "A photorealistic red panda coding on a laptop, golden hour";
    private const int TotalRunsPerDeployment = 3;
    private const int MeasuredRunStartIndex = 2;
    private const string GptImageApiVersion = "2025-04-01-preview";

    private readonly string _outputDirectory = Path.Combine(projectDirectory, "output");
    private readonly string _readmePath = Path.Combine(projectDirectory, "README.md");
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<string> RunAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_outputDirectory);

        var request = new ImageGenerationRequest(Prompt);
        var options = new Microsoft.Extensions.AI.ImageGenerationOptions
        {
            ImageSize = new Size(1024, 1024)
        };

        var deployments = new[]
        {
            new DeploymentDefinition(
                settings.MaiImage2Deployment,
                settings.MaiImage2Deployment,
                RunMaiImage2Async),
            new DeploymentDefinition(
                settings.GptImage2Deployment,
                settings.GptImage2Deployment,
                RunGptImage2Async)
        };

        var summaries = new List<DeploymentSummary>(deployments.Length);

        foreach (var deployment in deployments)
        {
            Console.WriteLine($"=== {deployment.DisplayName} ({deployment.DeploymentName}) ===");

            var measuredRuns = new List<RunMetric>(2);
            string? sampleFileName = null;

            for (var runNumber = 1; runNumber <= TotalRunsPerDeployment; runNumber++)
            {
                var metric = await deployment.RunAsync(request, options, runNumber, cancellationToken).ConfigureAwait(false);
                sampleFileName = metric.OutputFileName;

                var label = runNumber == 1 ? "warm-up" : $"measured #{runNumber - 1}";
                Console.WriteLine($"  {label,-11} latency={metric.LatencyMs}ms input={FormatTokens(metric.InputTokens)} output={FormatOutput(metric)} cost={FormatCost(metric.EstimatedCostUsd)}");

                if (runNumber >= MeasuredRunStartIndex)
                {
                    measuredRuns.Add(metric);
                }
            }

            summaries.Add(DeploymentSummary.FromRuns(measuredRuns, deployment.DeploymentName, sampleFileName ?? string.Empty));
            Console.WriteLine();
        }

        var markdown = BuildReadme(summaries);
        await File.WriteAllTextAsync(_readmePath, markdown, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"README written to: {_readmePath}");
        return markdown;
    }

    private async Task<RunMetric> RunMaiImage2Async(ImageGenerationRequest request, Microsoft.Extensions.AI.ImageGenerationOptions options, int runNumber, CancellationToken cancellationToken)
    {
        var outputFileName = $"{settings.MaiImage2Deployment}-{runNumber}.png";
        var outputPath = Path.Combine(_outputDirectory, outputFileName);
        var payload = new MaiImage2Request(
            settings.MaiImage2Deployment,
            request.Prompt ?? string.Empty,
            options.ImageSize?.Width ?? 1024,
            options.ImageSize?.Height ?? 1024);

        using var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMinutes(10);
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{settings.Endpoint.TrimEnd('/')}/mai/v1/images/generations")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, FoundryComparisonJsonContext.Default.MaiImage2Request), Encoding.UTF8, "application/json")
        };

        await authHeaderProvider.ApplyAsync(requestMessage, cancellationToken).ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"MAI-Image-2 request failed with {(int)response.StatusCode} {response.StatusCode}: {responseText}");
        }

        var result = JsonSerializer.Deserialize(responseText, FoundryComparisonJsonContext.Default.MaiImage2Response)
            ?? throw new InvalidOperationException("MAI-Image-2 response body was empty.");

        var base64Image = result.Data.FirstOrDefault()?.Base64Image
            ?? throw new InvalidOperationException("MAI-Image-2 response did not contain image data.");

        await File.WriteAllBytesAsync(outputPath, Convert.FromBase64String(base64Image), cancellationToken).ConfigureAwait(false);

        var estimatedCost = result.Usage is { InputTokens: { } inputTokens, OutputTokens: { } outputTokens }
            ? ((inputTokens * Pricing.MaiImage2InputUsdPerMillionTokens) + (outputTokens * Pricing.MaiImage2OutputUsdPerMillionTokens)) / 1_000_000m
            : Pricing.MaiImage2EstimatedUsdPerImage1024;

        return new RunMetric(
            outputFileName,
            stopwatch.ElapsedMilliseconds,
            result.Usage?.InputTokens,
            result.Usage?.OutputTokens,
            result.Data.Count,
            estimatedCost);
    }

    private async Task<RunMetric> RunGptImage2Async(ImageGenerationRequest request, Microsoft.Extensions.AI.ImageGenerationOptions options, int runNumber, CancellationToken cancellationToken)
    {
        var outputFileName = $"{settings.GptImage2Deployment}-{runNumber}.png";
        var outputPath = Path.Combine(_outputDirectory, outputFileName);
        var size = $"{options.ImageSize?.Width ?? 1024}x{options.ImageSize?.Height ?? 1024}";
        var payload = new GptImage2Request(
            request.Prompt ?? string.Empty,
            1,
            size,
            "png");

        using var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMinutes(10);
        using var requestMessage = new HttpRequestMessage(
            HttpMethod.Post,
            $"{settings.Endpoint.TrimEnd('/')}/openai/deployments/{settings.GptImage2Deployment}/images/generations?api-version={GptImageApiVersion}")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, FoundryComparisonJsonContext.Default.GptImage2Request), Encoding.UTF8, "application/json")
        };

        await authHeaderProvider.ApplyAsync(requestMessage, cancellationToken).ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();
        using var response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"GPT-Image-2 request failed with {(int)response.StatusCode} {response.StatusCode}: {responseText}");
        }

        var result = JsonSerializer.Deserialize(responseText, FoundryComparisonJsonContext.Default.GptImage2Response)
            ?? throw new InvalidOperationException("GPT-Image-2 response body was empty.");

        var base64Image = result.Data.FirstOrDefault()?.Base64Image
            ?? throw new InvalidOperationException("GPT-Image-2 response did not contain image data.");

        await File.WriteAllBytesAsync(outputPath, Convert.FromBase64String(base64Image), cancellationToken).ConfigureAwait(false);

        decimal? estimatedCost = result.Usage is { InputTokens: { } inputTokens, OutputTokens: { } outputTokens }
            ? Pricing.CalculateGptImage2Cost(inputTokens, outputTokens)
            : null;

        return new RunMetric(
            outputFileName,
            stopwatch.ElapsedMilliseconds,
            result.Usage?.InputTokens,
            result.Usage?.OutputTokens,
            result.Data.Count,
            estimatedCost);
    }

    private string BuildReadme(IReadOnlyList<DeploymentSummary> summaries)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# scenario-foundry-comparison");
        builder.AppendLine();
        builder.AppendLine($"Prompt: `{Prompt}`");
        builder.AppendLine();
        builder.AppendLine("| Deployment | Avg latency (ms) | Tokens in | Tokens out | Cost / image (USD) | Sample |");
        builder.AppendLine("|-----------|------------------|-----------|------------|---------------------|--------|");

        foreach (var summary in summaries)
        {
            builder.AppendLine($"| {summary.DeploymentName} | {summary.AverageLatencyMs} | {summary.TokensInDisplay} | {summary.TokensOutDisplay} | {summary.CostDisplay} | <img src=\"output/{summary.SampleFileName}\" width=\"160\" alt=\"{summary.DeploymentName} sample\" /> |");
        }

        builder.AppendLine();
        builder.AppendLine("> Benchmarks use 1 warm-up run and 2 measured runs per deployment.");
        builder.AppendLine("> MAI-Image-2 currently does not return token usage in the image response, so the cost shown is the published 1024x1024 estimate and token columns stay `N/A` when usage is absent.");
        return builder.ToString();
    }

    private static string FormatTokens(int? value) => value?.ToString() ?? "N/A";

    private static string FormatOutput(RunMetric metric) =>
        metric.OutputTokens is int outputTokens
            ? outputTokens.ToString()
            : FormatImageCount(metric.OutputCount);

    private static string FormatCost(decimal? value) => value is null ? "N/A" : value.Value.ToString("0.0000");

    internal static string FormatImageCount(double count) =>
        Math.Abs(count - 1d) < 0.001d ? "1 image" : $"{count:0.#} images";
}

internal sealed class FoundryAuthHeaderProvider
{
    private static readonly string[] Scopes = ["https://cognitiveservices.azure.com/.default"];
    private readonly DefaultAzureCredential _defaultAzureCredential = new();
    private readonly string? _apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_API_KEY");
    private AccessToken _accessToken;

    public async Task ApplyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.TryAddWithoutValidation("api-key", _apiKey);
            return;
        }

        if (_accessToken.ExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            _accessToken = await _defaultAzureCredential
                .GetTokenAsync(new TokenRequestContext(Scopes), cancellationToken)
                .ConfigureAwait(false);
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken.Token);
    }
}

internal sealed record DeploymentDefinition(
    string DeploymentName,
    string DisplayName,
    Func<ImageGenerationRequest, Microsoft.Extensions.AI.ImageGenerationOptions, int, CancellationToken, Task<RunMetric>> RunAsync);

internal sealed record RunMetric(
    string OutputFileName,
    long LatencyMs,
    int? InputTokens,
    int? OutputTokens,
    int OutputCount,
    decimal? EstimatedCostUsd);

internal sealed record DeploymentSummary(
    string DeploymentName,
    long AverageLatencyMs,
    string TokensInDisplay,
    string TokensOutDisplay,
    string CostDisplay,
    string SampleFileName)
{
    public static DeploymentSummary FromRuns(IReadOnlyList<RunMetric> measuredRuns, string deploymentName, string sampleFileName)
    {
        var averageLatency = (long)Math.Round(measuredRuns.Average(run => run.LatencyMs));
        var averageInputTokens = measuredRuns.All(run => run.InputTokens.HasValue)
            ? (int?)Math.Round(measuredRuns.Average(run => run.InputTokens!.Value))
            : null;
        var averageOutputTokens = measuredRuns.All(run => run.OutputTokens.HasValue)
            ? (int?)Math.Round(measuredRuns.Average(run => run.OutputTokens!.Value))
            : null;
        decimal? averageCost = measuredRuns.All(run => run.EstimatedCostUsd.HasValue)
            ? measuredRuns.Average(run => run.EstimatedCostUsd!.Value)
            : null;

        return new DeploymentSummary(
            deploymentName,
            averageLatency,
            averageInputTokens?.ToString() ?? "N/A",
            averageOutputTokens?.ToString() ?? FoundryComparisonRunner.FormatImageCount(measuredRuns.Average(run => run.OutputCount)),
            averageCost?.ToString("0.0000") ?? "N/A",
            sampleFileName);
    }
}

internal sealed record FoundryComparisonSettings(
    string Endpoint,
    string MaiImage2Deployment,
    string GptImage2Deployment)
{
    public static FoundryComparisonSettings Load(IConfiguration configuration)
    {
        var endpoint = configuration["AZURE_FOUNDRY_ENDPOINT"];
        var maiImage2Deployment = configuration["MAI_IMAGE_2_DEPLOYMENT"];
        var gptImage2Deployment = configuration["GPT_IMAGE_2_DEPLOYMENT"];

        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(maiImage2Deployment) ||
            string.IsNullOrWhiteSpace(gptImage2Deployment))
        {
            throw new InvalidOperationException(
                "Set AZURE_FOUNDRY_ENDPOINT, MAI_IMAGE_2_DEPLOYMENT, and GPT_IMAGE_2_DEPLOYMENT before running this sample. " +
                "Use AZURE_FOUNDRY_API_KEY or let DefaultAzureCredential authenticate locally.");
        }

        return new FoundryComparisonSettings(endpoint, maiImage2Deployment, gptImage2Deployment);
    }
}

internal sealed record MaiImage2Request(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

internal sealed record GptImage2Request(
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("n")] int Count,
    [property: JsonPropertyName("size")] string Size,
    [property: JsonPropertyName("output_format")] string OutputFormat);

internal sealed record MaiImage2Response(
    [property: JsonPropertyName("data")] IReadOnlyList<ImageDataItem> Data,
    [property: JsonPropertyName("usage")] UsageResponse? Usage);

internal sealed record GptImage2Response(
    [property: JsonPropertyName("data")] IReadOnlyList<ImageDataItem> Data,
    [property: JsonPropertyName("usage")] UsageResponse? Usage);

internal sealed record ImageDataItem(
    [property: JsonPropertyName("b64_json")] string? Base64Image);

internal sealed record UsageResponse(
    [property: JsonPropertyName("input_tokens")] int? InputTokens,
    [property: JsonPropertyName("output_tokens")] int? OutputTokens);

[JsonSerializable(typeof(MaiImage2Request))]
[JsonSerializable(typeof(GptImage2Request))]
[JsonSerializable(typeof(MaiImage2Response))]
[JsonSerializable(typeof(GptImage2Response))]
internal sealed partial class FoundryComparisonJsonContext : JsonSerializerContext;
