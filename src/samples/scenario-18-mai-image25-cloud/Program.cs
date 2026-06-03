using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== ElBruno.Text2Image - MAI-Image-2.5 Cloud API Demo (Microsoft Foundry) ===");
Console.WriteLine();

// Build configuration: User Secrets > Environment Variables > appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

// Read from configuration (supports user secrets, env vars, and appsettings.json)
var endpoint = config["MAI_IMAGE25_ENDPOINT"];
var apiKey = config["MAI_IMAGE25_API_KEY"];

// Model name sent in the API request body.
// Use "MAI-Image-2.5" (quality) or "MAI-Image-2.5-Flash" (speed-optimized).
var modelId = config["MAI_IMAGE25_MODEL_ID"] ?? "MAI-Image-2.5";

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("ERROR: MAI_IMAGE25_ENDPOINT and MAI_IMAGE25_API_KEY are not configured.");
    Console.WriteLine();
    Console.WriteLine("Configure using one of these methods:");
    Console.WriteLine();
    Console.WriteLine("  Option 1 - User Secrets (recommended for development):");
    Console.WriteLine("    dotnet user-secrets set MAI_IMAGE25_ENDPOINT \"https://your-resource.services.ai.azure.com\"");
    Console.WriteLine("    dotnet user-secrets set MAI_IMAGE25_API_KEY \"your-api-key-here\"");
    Console.WriteLine("    dotnet user-secrets set MAI_IMAGE25_MODEL_ID \"MAI-Image-2.5\"");
    Console.WriteLine();
    Console.WriteLine("  Option 2 - Environment Variables:");
    Console.WriteLine("    set MAI_IMAGE25_ENDPOINT=https://your-resource.services.ai.azure.com");
    Console.WriteLine("    set MAI_IMAGE25_API_KEY=your-api-key-here");
    Console.WriteLine("    set MAI_IMAGE25_MODEL_ID=MAI-Image-2.5");
    Console.WriteLine();
    Console.WriteLine("To get endpoint and API key:");
    Console.WriteLine("  1. Go to Microsoft Foundry portal (https://ai.azure.com)");
    Console.WriteLine("  2. Deploy a MAI-Image-2.5 or MAI-Image-2.5-Flash model");
    Console.WriteLine("  3. Copy the .services.ai.azure.com endpoint URL and API key from the deployment");
    return;
}

// Create a MAI-Image-2.5 generator. The same class serves both variants —
// pass modelId "MAI-Image-2.5-Flash" for the speed-optimized model.
using var httpClient = new HttpClient();
using var generator = new MaiImage25Generator(
    endpoint,
    apiKey,
    httpClient,
    modelName: modelId,
    modelId: modelId);

Console.WriteLine($"Model: {generator.ModelName}");
Console.WriteLine($"Model ID: {generator.ModelId}");
Console.WriteLine($"Endpoint: {generator.Endpoint}");
Console.WriteLine("MAI-Image-2.5 cloud model ready (no download required)");
Console.WriteLine();

// Generate a sample image
var prompt = "A photograph of a red fox in an autumn forest";
Console.WriteLine($"Generating image for: \"{prompt}\"");
Console.WriteLine("Calling Microsoft Foundry API...");

try
{
    var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
    });

    var outputPath = "mai_image25_output.png";
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
