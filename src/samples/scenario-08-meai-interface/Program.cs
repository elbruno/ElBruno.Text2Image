using System.Drawing;
using Microsoft.Extensions.AI;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - Microsoft.Extensions.AI Interface ===");
Console.WriteLine();

// All generators implement Microsoft.Extensions.AI.IImageGenerator,
// so you can use the standard interface for a unified API.

// Create generator and cast to the M.E.AI interface
using var sd15 = new StableDiffusion15();
IImageGenerator generator = sd15;  // M.E.AI interface

Console.WriteLine("Ensuring model is available...");
await sd15.EnsureModelAvailableAsync();
Console.WriteLine("Model ready!");
Console.WriteLine();

// Use the standard M.E.AI ImageGenerationRequest
var request = new ImageGenerationRequest("a whimsical treehouse in an enchanted forest, fantasy art, vibrant colors");

// Configure with standard M.E.AI ImageGenerationOptions
var options = new ImageGenerationOptions
{
    ImageSize = new Size(512, 512),
    AdditionalProperties = new AdditionalPropertiesDictionary
    {
        ["num_inference_steps"] = 15,
        ["guidance_scale"] = 7.5,
        ["seed"] = 42
    }
};

Console.WriteLine($"Generating with M.E.AI interface...");
Console.WriteLine($"Prompt: \"{request.Prompt}\"");
Console.WriteLine($"Size: {options.ImageSize}");

var response = await generator.GenerateAsync(request, options);

// The response contains AIContent items — DataContent has the image bytes
var imageContent = response.Contents.OfType<DataContent>().First();
var imageBytes = imageContent.Data.ToArray();

// Save to file
var outputPath = "meai_output.png";
await File.WriteAllBytesAsync(outputPath, imageBytes);
Console.WriteLine();
Console.WriteLine($"Image saved to: {Path.GetFullPath(outputPath)}");
Console.WriteLine($"Content type: {imageContent.MediaType}");
Console.WriteLine($"Image size: {imageBytes.Length / 1024.0:F1} KB");

// You can also access the original result via RawRepresentation
if (response.RawRepresentation is ElBruno.Text2Image.ImageGenerationResult raw)
{
    Console.WriteLine($"Seed: {raw.Seed}");
    Console.WriteLine($"Inference time: {raw.InferenceTimeMs}ms");
}
