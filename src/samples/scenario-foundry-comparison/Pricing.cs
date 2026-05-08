namespace ElBruno.Text2Image.Samples.FoundryComparison;

internal static class Pricing
{
    // Source: https://azure.microsoft.com/pricing/details/ai-foundry/
    public const decimal GptImage2InputUsdPerMillionTokens = 5.0m;

    // Source: https://azure.microsoft.com/pricing/details/ai-foundry/
    public const decimal GptImage2OutputUsdPerMillionTokens = 10.0m;

    // Source: https://azure.microsoft.com/pricing/details/ai-foundry/
    public const decimal MaiImage2InputUsdPerMillionTokens = 5.0m;

    // Source: https://azure.microsoft.com/pricing/details/ai-foundry/
    public const decimal MaiImage2OutputUsdPerMillionTokens = 33.0m;

    // MAI pricing is published per token, but the MAI image API does not currently return usage in the response.
    // This fixed 1024x1024 estimate is used until the API exposes per-call token counts.
    public const decimal MaiImage2EstimatedUsdPerImage1024 = 0.0340m;

    public static decimal CalculateGptImage2Cost(int inputTokens, int outputTokens) =>
        ((inputTokens * GptImage2InputUsdPerMillionTokens) +
         (outputTokens * GptImage2OutputUsdPerMillionTokens)) / 1_000_000m;
}
