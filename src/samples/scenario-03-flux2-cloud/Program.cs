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

// Optional: model ID for model-based endpoints (e.g., "FLUX.2-pro", "FLUX.2-flex")
// For deployment-based endpoints (model in URL), this can be omitted.
var modelId = config["FLUX2_MODEL_ID"];

// Model name defaults to "FLUX.2-flex" — override via FLUX2_MODEL_NAME user secret
var modelName = config["FLUX2_MODEL_NAME"] ?? "FLUX.2-flex";

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("ERROR: FLUX2_ENDPOINT and FLUX2_API_KEY are not configured.");
    Console.WriteLine();
    Console.WriteLine("Configure using one of these methods:");
    Console.WriteLine();
    Console.WriteLine("  Option 1 - User Secrets (recommended for development):");
    Console.WriteLine("    dotnet user-secrets set FLUX2_ENDPOINT \"https://your-resource.services.ai.azure.com/...\"");
    Console.WriteLine("    dotnet user-secrets set FLUX2_API_KEY \"your-api-key-here\"");
    Console.WriteLine();
    Console.WriteLine("  Optional model configuration:");
    Console.WriteLine("    dotnet user-secrets set FLUX2_MODEL_NAME \"FLUX.2-flex\"  # default value");
    Console.WriteLine("    dotnet user-secrets set FLUX2_MODEL_ID \"FLUX.2-flex\"    # or \"FLUX.2-pro\"");
    Console.WriteLine();
    Console.WriteLine("  Option 2 - Environment Variables:");
    Console.WriteLine("    set FLUX2_ENDPOINT=https://your-resource.services.ai.azure.com/...");
    Console.WriteLine("    set FLUX2_API_KEY=your-api-key-here");
    Console.WriteLine("    set FLUX2_MODEL_ID=FLUX.2-pro    (optional)");
    Console.WriteLine();
    Console.WriteLine("  Option 3 - appsettings.json:");
    Console.WriteLine("    { \"FLUX2_ENDPOINT\": \"...\", \"FLUX2_API_KEY\": \"...\", \"FLUX2_MODEL_ID\": \"FLUX.2-pro\" }");
    Console.WriteLine();
    Console.WriteLine("  Available model IDs:");
    Console.WriteLine("    FLUX.2-pro  — Photorealistic image generation");
    Console.WriteLine("    FLUX.2-flex — Text-heavy design and UI prototyping");
    Console.WriteLine();
    Console.WriteLine("To get endpoint and API key:");
    Console.WriteLine("  1. Go to Microsoft Foundry portal (https://ai.azure.com)");
    Console.WriteLine("  2. Deploy a FLUX.2 model");
    Console.WriteLine("  3. Copy the endpoint URL and API key from the deployment");
    return;
}

// Create a FLUX.2 generator
// - modelId is sent in the request body (required for model-based endpoints)
// - modelName is just a display label
using var generator = new Flux2Generator(endpoint, apiKey, modelName: modelName, modelId: modelId);

Console.WriteLine($"Model: {generator.ModelName}");
if (generator.ModelId != null)
    Console.WriteLine($"Model ID: {generator.ModelId}");
Console.WriteLine("FLUX.2 cloud model ready (no download required)");
Console.WriteLine();

// Generate an image
var prompt = "A professional logo design for a tech startup called 'NovaTech' with clean modern typography, blue and silver color scheme";
Console.WriteLine($"Generating image for: \"{prompt}\"");
Console.WriteLine("Calling Microsoft Foundry API...");

try
{
    var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
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
