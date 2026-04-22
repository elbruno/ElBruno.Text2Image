using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== ElBruno.Text2Image - MAI-Image-2 Cloud API Demo (Microsoft Foundry) ===");
Console.WriteLine();

// Build configuration: User Secrets > Environment Variables > appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

// Read from configuration (supports user secrets, env vars, and appsettings.json)
var endpoint = config["MAI_IMAGE2_ENDPOINT"];
var apiKey = config["MAI_IMAGE2_API_KEY"];

// Model/deployment name sent in the API request body
var modelId = config["MAI_IMAGE2_MODEL_ID"] ?? "mai-image-2";

// Display name (for logging/UI)
var modelName = config["MAI_IMAGE2_MODEL_NAME"] ?? "MAI-Image-2";

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("ERROR: MAI_IMAGE2_ENDPOINT and MAI_IMAGE2_API_KEY are not configured.");
    Console.WriteLine();
    Console.WriteLine("Configure using one of these methods:");
    Console.WriteLine();
    Console.WriteLine("  Option 1 - User Secrets (recommended for development):");
    Console.WriteLine("    dotnet user-secrets set MAI_IMAGE2_ENDPOINT \"https://your-resource.services.ai.azure.com\"");
    Console.WriteLine("    dotnet user-secrets set MAI_IMAGE2_API_KEY \"your-api-key-here\"");
    Console.WriteLine("    dotnet user-secrets set MAI_IMAGE2_MODEL_NAME \"MAI-Image-2\"");
    Console.WriteLine("    dotnet user-secrets set MAI_IMAGE2_MODEL_ID \"mai-image-2\"");
    Console.WriteLine();
    Console.WriteLine("  Option 2 - Environment Variables:");
    Console.WriteLine("    set MAI_IMAGE2_ENDPOINT=https://your-resource.services.ai.azure.com");
    Console.WriteLine("    set MAI_IMAGE2_API_KEY=your-api-key-here");
    Console.WriteLine("    set MAI_IMAGE2_MODEL_ID=mai-image-2");
    Console.WriteLine();
    Console.WriteLine("  Option 3 - appsettings.json:");
    Console.WriteLine("    { \"MAI_IMAGE2_ENDPOINT\": \"...\", \"MAI_IMAGE2_API_KEY\": \"...\", \"MAI_IMAGE2_MODEL_ID\": \"mai-image-2\" }");
    Console.WriteLine();
    Console.WriteLine("To get endpoint and API key:");
    Console.WriteLine("  1. Go to Microsoft Foundry portal (https://ai.azure.com)");
    Console.WriteLine("  2. Deploy an MAI-Image-2 model");
    Console.WriteLine("  3. Copy the .services.ai.azure.com endpoint URL and API key from the deployment");
    return;
}

// Create an MAI-Image-2 generator
// - modelId is the deployment/model name sent in the request body
// - modelName is just a display label

// Uncomment the next line to use a custom HttpClient with a longer timeout (e.g., 600 seconds)
// var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(600) };
// using var generator = new MaiImage2Generator(endpoint, apiKey, httpClient, modelName: modelName, modelId: modelId);

using var httpClient = new HttpClient();
using var generator = new MaiImage2Generator(endpoint, apiKey, httpClient, modelName: modelName, modelId: modelId);

Console.WriteLine($"Model: {generator.ModelName}");
Console.WriteLine($"Model ID: {generator.ModelId}");
Console.WriteLine($"Endpoint: {generator.Endpoint}");
Console.WriteLine("MAI-Image-2 cloud model ready (no download required)");
Console.WriteLine();

// Generate a sample image
var prompt = "a simple flat icon of a paintbrush and a sparkle, purple and blue gradient, white background, minimal, square logo";
Console.WriteLine($"Generating image for: \"{prompt}\"");
Console.WriteLine("Calling Microsoft Foundry API...");

try
{
    var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
    });

    var outputPath = "mai_image2_output.png";
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
