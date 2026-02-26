using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== ElBruno.Text2Image - FLUX.2 Cloud API Demo (Microsoft Foundry) ===");
Console.WriteLine();

// Build configuration: User Secrets > Environment Variables > appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

// Read from configuration (supports user secrets, env vars, and appsettings.json)
var endpoint = config["FLUX2_ENDPOINT"];
var apiKey = config["FLUX2_API_KEY"];

// Model/deployment name sent in the API request body (e.g., "FLUX.2-pro", "FLUX.2-flex")
var modelId = config["FLUX2_MODEL_ID"] ?? "FLUX.2-pro";

// Display name (for logging/UI)
var modelName = config["FLUX2_MODEL_NAME"] ?? "FLUX.2-pro";

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("ERROR: FLUX2_ENDPOINT and FLUX2_API_KEY are not configured.");
    Console.WriteLine();
    Console.WriteLine("Configure using one of these methods:");
    Console.WriteLine();
    Console.WriteLine("  Option 1 - User Secrets (recommended for development):");
    Console.WriteLine("    dotnet user-secrets set FLUX2_ENDPOINT \"https://your-resource.services.ai.azure.com\"");
    Console.WriteLine("    dotnet user-secrets set FLUX2_API_KEY \"your-api-key-here\"");
    Console.WriteLine("    dotnet user-secrets set FLUX2_MODEL_NAME \"FLUX.2-pro\"");
    Console.WriteLine("    dotnet user-secrets set FLUX2_MODEL_ID \"FLUX.2-pro\"");
    Console.WriteLine();
    Console.WriteLine("  Option 2 - Environment Variables:");
    Console.WriteLine("    set FLUX2_ENDPOINT=https://your-resource.services.ai.azure.com");
    Console.WriteLine("    set FLUX2_API_KEY=your-api-key-here");
    Console.WriteLine("    set FLUX2_MODEL_ID=FLUX.2-pro");
    Console.WriteLine();
    Console.WriteLine("  Option 3 - appsettings.json:");
    Console.WriteLine("    { \"FLUX2_ENDPOINT\": \"...\", \"FLUX2_API_KEY\": \"...\", \"FLUX2_MODEL_ID\": \"FLUX.2-pro\" }");
    Console.WriteLine();
    Console.WriteLine("  Available model IDs:");
    Console.WriteLine("    FLUX.2-pro  — Photorealistic image generation (default)");
    Console.WriteLine("    FLUX.2-flex — Text-heavy design and UI prototyping");
    Console.WriteLine();
    Console.WriteLine("To get endpoint and API key:");
    Console.WriteLine("  1. Go to Microsoft Foundry portal (https://ai.azure.com)");
    Console.WriteLine("  2. Deploy a FLUX.2 model");
    Console.WriteLine("  3. Copy the .services.ai.azure.com endpoint URL and API key from the deployment");
    return;
}

// Create a FLUX.2 generator
// - modelId is the deployment/model name sent in the request body
// - modelName is just a display label

// Uncomment the next line to use a custom HttpClient with a longer timeout (e.g., 600 seconds)
// var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(600) };
// using var generator = new Flux2Generator(endpoint, apiKey, modelName: modelName, modelId: modelId, httpClient: httpClient);

using var generator = new Flux2Generator(endpoint, apiKey, modelName: modelName, modelId: modelId);

Console.WriteLine($"Model: {generator.ModelName}");
Console.WriteLine($"Model ID: {generator.ModelId}");
Console.WriteLine($"Endpoint: {generator.Endpoint}");
Console.WriteLine("FLUX.2 cloud model ready (no download required)");
Console.WriteLine();

// Generate a small logo image for the repository
var prompt = "a simple flat icon of a paintbrush and a sparkle, purple and blue gradient, white background, minimal, square logo";
Console.WriteLine($"Generating image for: \"{prompt}\"");
Console.WriteLine("Calling Microsoft Foundry API...");

try
{
    var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
    {
        Width = 512,
        Height = 512
    });

    var outputPath = "flux2_output.png";
    await result.SaveAsync(outputPath);
    Console.WriteLine();
    Console.WriteLine($"Image saved to: {Path.GetFullPath(outputPath)}");
    Console.WriteLine($"Inference time: {result.InferenceTimeMs}ms");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
}
