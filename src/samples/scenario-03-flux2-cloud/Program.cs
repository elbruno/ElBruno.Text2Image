using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

Console.WriteLine("=== ElBruno.Text2Image - FLUX.2 Cloud API Demo (Microsoft Foundry) ===");
Console.WriteLine();

// FLUX.2 requires a Microsoft Foundry deployment
// Set these environment variables before running:
//   FLUX2_ENDPOINT - Your Microsoft Foundry FLUX.2 endpoint URL
//   FLUX2_API_KEY  - Your API key
var endpoint = Environment.GetEnvironmentVariable("FLUX2_ENDPOINT");
var apiKey = Environment.GetEnvironmentVariable("FLUX2_API_KEY");

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("ERROR: Please set FLUX2_ENDPOINT and FLUX2_API_KEY environment variables.");
    Console.WriteLine();
    Console.WriteLine("To get these values:");
    Console.WriteLine("  1. Go to Microsoft Foundry portal");
    Console.WriteLine("  2. Deploy a FLUX.2 model (FLUX.2-pro or FLUX.2-flex)");
    Console.WriteLine("  3. Copy the endpoint URL and API key from the deployment");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  set FLUX2_ENDPOINT=https://myresource.services.ai.azure.com/openai/deployments/flux-2-pro/images/generations?api-version=2024-06-01");
    Console.WriteLine("  set FLUX2_API_KEY=your-api-key-here");
    return;
}

// Create a FLUX.2 generator (no model download needed — it's a cloud API)
using var generator = new Flux2Generator(endpoint, apiKey, modelName: "FLUX.2 Pro");

Console.WriteLine("FLUX.2 cloud model ready (no download required)");
Console.WriteLine();

// Generate an image
var prompt = "A professional logo design for a tech startup called 'NovaTech' with clean modern typography, blue and silver color scheme";
Console.WriteLine($"Generating image for: \"{prompt}\"");
Console.WriteLine("Calling Azure AI Foundry API...");

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
