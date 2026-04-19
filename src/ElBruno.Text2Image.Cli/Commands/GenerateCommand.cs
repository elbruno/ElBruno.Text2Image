using System.ComponentModel;
using Spectre.Console.Cli;

namespace ElBruno.Text2Image.Cli.Commands;

/// <summary>
/// Main image generation command.
/// Usage: t2i "a cat" [--provider cpu] [--out output.png] [--width 512] [--height 512] [--steps 20]
/// </summary>
internal sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<prompt>")]
        [Description("The text prompt describing the image to generate")]
        public string Prompt { get; init; } = string.Empty;

        [CommandOption("--provider")]
        [Description("Provider to use (cpu, cuda, directml, foundry-flux2, foundry-mai2)")]
        public string? Provider { get; init; }

        [CommandOption("--out|-o")]
        [Description("Output file path (default: output.png)")]
        public string? OutputPath { get; init; }

        [CommandOption("--width|-w")]
        [Description("Image width in pixels (default: 512)")]
        [DefaultValue(512)]
        public int Width { get; init; } = 512;

        [CommandOption("--height|-h")]
        [Description("Image height in pixels (default: 512)")]
        [DefaultValue(512)]
        public int Height { get; init; } = 512;

        [CommandOption("--steps|-s")]
        [Description("Number of inference steps (default: 20)")]
        [DefaultValue(20)]
        public int Steps { get; init; } = 20;

        [CommandOption("--endpoint")]
        [Description("Cloud provider endpoint (override config)")]
        public string? Endpoint { get; init; }

        [CommandOption("--api-key")]
        [Description("Cloud provider API key (override secrets)")]
        public string? ApiKey { get; init; }
    }

    // TODO(Kaylee): implement generation logic
    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        throw new NotImplementedException("GenerateCommand not yet implemented");
    }
}
