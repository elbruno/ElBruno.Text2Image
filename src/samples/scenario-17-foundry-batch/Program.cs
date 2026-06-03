using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== ElBruno.Text2Image - Foundry Batch Generation ===");
Console.WriteLine();

// Build configuration: User Secrets > Environment Variables > appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

var endpoint = config["FLUX2_ENDPOINT"];
var apiKey = config["FLUX2_API_KEY"];
var modelId = config["FLUX2_MODEL_ID"] ?? "FLUX.2-pro";
var modelName = config["FLUX2_MODEL_NAME"] ?? "FLUX.2-pro";

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("ERROR: FLUX2_ENDPOINT and FLUX2_API_KEY are not configured.");
    Console.WriteLine();
    Console.WriteLine("Configure with user secrets (recommended):");
    Console.WriteLine("  dotnet user-secrets set FLUX2_ENDPOINT \"https://your-resource.services.ai.azure.com\"");
    Console.WriteLine("  dotnet user-secrets set FLUX2_API_KEY \"your-api-key-here\"");
    Console.WriteLine("  dotnet user-secrets set FLUX2_MODEL_ID \"FLUX.2-pro\"");
    Console.WriteLine();
    Console.WriteLine("Or set environment variables FLUX2_ENDPOINT and FLUX2_API_KEY.");
    return;
}

using var httpClient = new HttpClient();
using var generator = new Flux2Generator(endpoint, apiKey, httpClient, modelName: modelName, modelId: modelId);

Console.WriteLine($"Model: {generator.ModelName}");
Console.WriteLine($"Model ID: {generator.ModelId}");
Console.WriteLine($"Endpoint: {generator.Endpoint}");
Console.WriteLine();

var prompts = new[]
{
    "a futuristic city skyline at sunset, cyberpunk, neon lights",
    "a peaceful meadow with wildflowers and butterflies, impressionist painting",
    "an astronaut floating in space with Earth in the background, photorealistic",
    "a steampunk clockwork dragon, intricate mechanical details, brass and copper",
    "a cozy coffee shop interior on a rainy day, warm lighting, watercolor"
};

var outputDir = "foundry_batch_output";
Directory.CreateDirectory(outputDir);

var options = new ImageGenerationOptions
{
    Width = 512,
    Height = 512
};

Console.WriteLine($"Generating {prompts.Length} images using Microsoft Foundry...");
Console.WriteLine();

for (int i = 0; i < prompts.Length; i++)
{
    var prompt = prompts[i];
    Console.WriteLine($"[{i + 1}/{prompts.Length}] \"{prompt[..Math.Min(60, prompt.Length)]}...\"");

    try
    {
        var result = await generator.GenerateAsync(prompt, options);
        var filename = Path.Combine(outputDir, $"foundry_batch_{i + 1:D2}.png");
        await result.SaveAsync(filename);
        Console.WriteLine($"  Saved to {filename} ({result.InferenceTimeMs}ms)");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"  API Error: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error: {ex.GetType().Name}: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"Done! {prompts.Length} images processed. Output folder: {Path.GetFullPath(outputDir)}");
