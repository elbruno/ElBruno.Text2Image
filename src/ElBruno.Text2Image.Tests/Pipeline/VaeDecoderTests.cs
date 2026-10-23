using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using ElBruno.Text2Image.Pipeline;
using Microsoft.ML.OnnxRuntime.Tensors;
using Xunit;

namespace ElBruno.Text2Image.Tests.Pipeline;

public class VaeDecoderTests
{
    [Fact]
    public void ConvertToImage_EncodesExpectedRgbaPixelsAsPng()
    {
        var tensor = new DenseTensor<float>(
            new[] { -1f, -2f, 0f, 2f, 1f, 0.25f },
            new[] { 1, 3, 1, 2 });

        var png = VaeDecoder.ConvertToImage(tensor, 2, 1);
        var (width, height, pixels) = DecodeRgbaPng(png);

        Assert.Equal(2, width);
        Assert.Equal(1, height);
        Assert.Equal(new byte[] { 0, 128, 255, 255, 0, 255, 159, 255 }, pixels);
    }

    private static (int Width, int Height, byte[] Pixels) DecodeRgbaPng(byte[] png)
    {
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png[..8]);

        int width = 0;
        int height = 0;
        bool foundEnd = false;
        var compressedData = new MemoryStream();

        for (int offset = 8; offset < png.Length;)
        {
            int chunkLength = checked((int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset, 4)));
            string chunkType = Encoding.ASCII.GetString(png, offset + 4, 4);
            ReadOnlySpan<byte> chunkData = png.AsSpan(offset + 8, chunkLength);
            uint expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset + 8 + chunkLength, 4));
            Assert.Equal(expectedCrc, ComputeCrc(png.AsSpan(offset + 4, 4 + chunkLength)));

            if (chunkType == "IHDR")
            {
                width = checked((int)BinaryPrimitives.ReadUInt32BigEndian(chunkData));
                height = checked((int)BinaryPrimitives.ReadUInt32BigEndian(chunkData[4..]));
                Assert.Equal(8, chunkData[8]);
                Assert.Equal(6, chunkData[9]);
            }
            else if (chunkType == "IDAT")
            {
                compressedData.Write(chunkData);
            }
            else if (chunkType == "IEND")
            {
                foundEnd = true;
                break;
            }

            offset = checked(offset + 12 + chunkLength);
        }

        Assert.True(foundEnd);
        compressedData.Position = 0;
        using var zlib = new ZLibStream(compressedData, CompressionMode.Decompress);
        using var scanlines = new MemoryStream();
        zlib.CopyTo(scanlines);

        int bytesPerPixel = 4;
        int stride = width * bytesPerPixel;
        byte[] filteredPixels = scanlines.ToArray();
        var pixels = new byte[height * stride];
        var previousRow = new byte[stride];

        for (int y = 0; y < height; y++)
        {
            int scanlineOffset = y * (stride + 1);
            byte filter = filteredPixels[scanlineOffset];
            var row = pixels.AsSpan(y * stride, stride);
            ReadOnlySpan<byte> filteredRow = filteredPixels.AsSpan(scanlineOffset + 1, stride);

            for (int i = 0; i < stride; i++)
            {
                byte left = i >= bytesPerPixel ? row[i - bytesPerPixel] : (byte)0;
                byte above = previousRow[i];
                byte upperLeft = i >= bytesPerPixel ? previousRow[i - bytesPerPixel] : (byte)0;
                int predictor = filter switch
                {
                    0 => 0,
                    1 => left,
                    2 => above,
                    3 => (left + above) / 2,
                    4 => PaethPredictor(left, above, upperLeft),
                    _ => throw new InvalidDataException($"Unsupported PNG filter: {filter}.")
                };

                row[i] = unchecked((byte)(filteredRow[i] + predictor));
            }

            row.CopyTo(previousRow);
        }

        return (width, height, pixels);
    }

    private static uint ComputeCrc(ReadOnlySpan<byte> data)
    {
        uint crc = uint.MaxValue;

        foreach (byte value in data)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320u : crc >> 1;
            }
        }

        return ~crc;
    }

    private static int PaethPredictor(byte left, byte above, byte upperLeft)
    {
        int estimate = left + above - upperLeft;
        int leftDistance = Math.Abs(estimate - left);
        int aboveDistance = Math.Abs(estimate - above);
        int upperLeftDistance = Math.Abs(estimate - upperLeft);

        return leftDistance <= aboveDistance && leftDistance <= upperLeftDistance
            ? left
            : aboveDistance <= upperLeftDistance ? above : upperLeft;
    }
}
