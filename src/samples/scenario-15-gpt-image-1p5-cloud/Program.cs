using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Configuration;

Console.WriteLine("=== ElBruno.Text2Image - GPT-Image-1.5 Cloud API Demo (Azure OpenAI) ===");
Console.WriteLine();

// Build configuration: User Secrets > Environment Variables > appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

// Read from configuration (supports user secrets, env vars, and appsettings.json)
var endpoint = config["GptImage1p5:Endpoint"];
var apiKey = config["GptImage1p5:ApiKey"];
var model = config["GptImage1p5:Model"] ?? "gpt-image-1p5";

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("ERROR: GptImage1p5:Endpoint and GptImage1p5:ApiKey are not configured.");
    Console.WriteLine();
    Console.WriteLine("Configure using one of these methods:");
    Console.WriteLine();
    Console.WriteLine("  Option 1 - User Secrets (recommended for development):");
    Console.WriteLine("    dotnet user-secrets set GptImage1p5:Endpoint \"https://your-resource.openai.azure.com\"");
    Console.WriteLine("    dotnet user-secrets set GptImage1p5:ApiKey \"your-api-key-here\"");
    Console.WriteLine("    dotnet user-secrets set GptImage1p5:Model \"gpt-image-1p5\"");
    Console.WriteLine();
    Console.WriteLine("  Option 2 - Environment Variables:");
    Console.WriteLine("    set GptImage1p5__Endpoint=https://your-resource.openai.azure.com");
    Console.WriteLine("    set GptImage1p5__ApiKey=your-api-key-here");
    Console.WriteLine("    set GptImage1p5__Model=gpt-image-1p5");
    Console.WriteLine();
    Console.WriteLine("  Option 3 - appsettings.json:");
    Console.WriteLine("    { \"GptImage1p5\": { \"Endpoint\": \"...\", \"ApiKey\": \"...\", \"Model\": \"gpt-image-1p5\" } }");
    Console.WriteLine();
    Console.WriteLine("To get endpoint and API key:");
    Console.WriteLine("  1. Go to Azure Portal (https://portal.azure.com)");
    Console.WriteLine("  2. Create or navigate to your Azure OpenAI resource");
    Console.WriteLine("  3. Go to Deployments and create/select a gpt-image-1.5 deployment");
    Console.WriteLine("  4. Copy the endpoint URL from the resource's Keys + Endpoint section");
    Console.WriteLine("  5. Copy your API key from the same section");
    return;
}

// Create a GPT-Image-1.5 generator
using var generator = new GptImage1p5Generator(endpoint, apiKey, modelName: "GPT-Image-1.5", deploymentName: model);

Console.WriteLine($"Model: {generator.ModelName}");
Console.WriteLine($"Deployment: {generator.DeploymentName}");
Console.WriteLine($"Endpoint: {generator.Endpoint}");
Console.WriteLine("GPT-Image-1.5 cloud model ready");
Console.WriteLine();

// Create output directory if it doesn't exist
var outputDir = "output";
Directory.CreateDirectory(outputDir);

// Scenario A: Basic generation (1024x1024)
Console.WriteLine("--- Scenario A: Basic Generation (1024x1024) ---");
var promptA = "a serene landscape with rolling hills, wildflowers, and a clear blue sky at sunset";
Console.WriteLine($"Prompt: \"{promptA}\"");
Console.WriteLine("Calling Azure OpenAI API...");

try
{
    var resultA = await generator.GenerateAsync(promptA, new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
    });

    var outputPathA = Path.Combine(outputDir, "scenario-a-1024x1024.png");
    await resultA.SaveAsync(outputPathA);
    Console.WriteLine($"✓ Image saved to: {Path.GetFullPath(outputPathA)}");
    Console.WriteLine($"  Inference time: {resultA.InferenceTimeMs}ms");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"✗ API Error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.GetType().Name}: {ex.Message}");
}

Console.WriteLine();

// Scenario B: Landscape variation (1792x1024)
Console.WriteLine("--- Scenario B: Landscape Variation (1792x1024) ---");
var promptB = "a modern city skyline at night with neon lights reflecting in a river, wide panoramic view";
Console.WriteLine($"Prompt: \"{promptB}\"");
Console.WriteLine("Calling Azure OpenAI API...");

try
{
    var resultB = await generator.GenerateAsync(promptB, new ImageGenerationOptions
    {
        Width = 1792,
        Height = 1024
    });

    var outputPathB = Path.Combine(outputDir, "scenario-b-1792x1024.png");
    await resultB.SaveAsync(outputPathB);
    Console.WriteLine($"✓ Image saved to: {Path.GetFullPath(outputPathB)}");
    Console.WriteLine($"  Inference time: {resultB.InferenceTimeMs}ms");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"✗ API Error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.GetType().Name}: {ex.Message}");
}

Console.WriteLine();

// Scenario C: Different prompt styles (1024x1024)
Console.WriteLine("--- Scenario C: Abstract Art Style (1024x1024) ---");
var promptC = "abstract geometric pattern with vibrant gradient colors, flowing shapes, digital art, professional, 4k quality";
Console.WriteLine($"Prompt: \"{promptC}\"");
Console.WriteLine("Calling Azure OpenAI API...");

try
{
    var resultC = await generator.GenerateAsync(promptC, new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
    });

    var outputPathC = Path.Combine(outputDir, "scenario-c-abstract.png");
    await resultC.SaveAsync(outputPathC);
    Console.WriteLine($"✓ Image saved to: {Path.GetFullPath(outputPathC)}");
    Console.WriteLine($"  Inference time: {resultC.InferenceTimeMs}ms");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"✗ API Error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.GetType().Name}: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("=== Generation Complete ===");
Console.WriteLine($"All images saved to: {Path.GetFullPath(outputDir)}");
