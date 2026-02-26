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
    Console.WriteLine("  Option 2 - Environment Variables:");
    Console.WriteLine("    set FLUX2_ENDPOINT=https://your-resource.services.ai.azure.com/...");
    Console.WriteLine("    set FLUX2_API_KEY=your-api-key-here");
    Console.WriteLine();
    Console.WriteLine("  Option 3 - appsettings.json:");
    Console.WriteLine("    Create appsettings.json with: { \"FLUX2_ENDPOINT\": \"...\", \"FLUX2_API_KEY\": \"...\" }");
    Console.WriteLine();
    Console.WriteLine("To get these values:");
    Console.WriteLine("  1. Go to Microsoft Foundry portal (https://ai.azure.com)");
    Console.WriteLine("  2. Deploy a FLUX.2 model (FLUX.2-pro or FLUX.2-flex)");
    Console.WriteLine("  3. Copy the endpoint URL and API key from the deployment");
    return;
}

// Create a FLUX.2 generator (no model download needed — it's a cloud API)
using var generator = new Flux2Generator(endpoint, apiKey, modelName: "FLUX.2 Pro");

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
