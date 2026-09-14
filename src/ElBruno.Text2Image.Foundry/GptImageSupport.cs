using OpenAI.Images;

namespace ElBruno.Text2Image.Foundry;

internal static class GptImageSupport
{
    public static (GeneratedImageSize Size, int Width, int Height) MapSize(int width, int height)
    {
        var requestedWidth = width > 0 ? width : 1024;
        var requestedHeight = height > 0 ? height : 1024;
        var aspectRatio = (double)requestedWidth / requestedHeight;

        if (aspectRatio > 1.2)
        {
            return (GeneratedImageSize.W1792xH1024, 1792, 1024);
        }

        if (aspectRatio < 0.85)
        {
            return (GeneratedImageSize.W1024xH1792, 1024, 1792);
        }

        return (GeneratedImageSize.W1024xH1024, 1024, 1024);
    }
}
