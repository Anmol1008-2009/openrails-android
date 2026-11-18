using System.Buffers.Binary;
using OpenRails.Core.Msts;

namespace OpenRails.Core.Formats;

/// <summary>
/// Decodes the common DirectDraw Surface formats used by MSTS/Open Rails
/// rolling stock. DXT1, DXT3, DXT5 and simple uncompressed 24/32-bit images
/// are supported. Only the top mip level is decoded.
/// </summary>
public sealed class DdsImage
{
    private const uint Magic = 0x20534444; // "DDS "
    private const uint HeaderSize = 124;
    private const uint PixelFormatFourCc = 0x4;
    private const uint PixelFormatRgb = 0x40;
    private const uint PixelFormatAlphaPixels = 0x1;

    private DdsImage(int width, int height, byte[] pixels, bool hasAlpha)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
        HasAlpha = hasAlpha;
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>RGBA8888 pixels, row-major, top-down.</summary>
    public byte[] Pixels { get; }

    public bool HasAlpha { get; }

    public static DdsImage Decode(ReadOnlySpan<byte> data, int maxDimension = 0)
    {
        if (data.Length < 128)
        {
            throw new MstsFormatException("DDS file is too small.");
        }

        if (BinaryPrimitives.ReadUInt32LittleEndian(data) != Magic)
        {
            throw new MstsFormatException("Not a DDS file.");
        }

        if (BinaryPrimitives.ReadUInt32LittleEndian(data[4..]) != HeaderSize)
        {
            throw new MstsFormatException("Unsupported DDS header size.");
        }

        var height = (int)BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
        var width = (int)BinaryPrimitives.ReadUInt32LittleEndian(data[16..]);
        if (width <= 0 || height <= 0 || width > 16384 || height > 16384)
        {
            throw new MstsFormatException($"Invalid DDS dimensions {width}x{height}.");
        }

        var pixelFormatSize = BinaryPrimitives.ReadUInt32LittleEndian(data[76..]);
        if (pixelFormatSize != 32)
        {
            throw new MstsFormatException("Unsupported DDS pixel format.");
        }

        var pixelFlags = BinaryPrimitives.ReadUInt32LittleEndian(data[80..]);
        var fourCc = BinaryPrimitives.ReadUInt32LittleEndian(data[84..]);
        var rgbBitCount = BinaryPrimitives.ReadUInt32LittleEndian(data[88..]);
        var redMask = BinaryPrimitives.ReadUInt32LittleEndian(data[92..]);
        var greenMask = BinaryPrimitives.ReadUInt32LittleEndian(data[96..]);
        var blueMask = BinaryPrimitives.ReadUInt32LittleEndian(data[100..]);
        var alphaMask = BinaryPrimitives.ReadUInt32LittleEndian(data[104..]);

        var format = "DXT1";
        var dataOffset = 128;
        var hasAlpha = false;

        if ((pixelFlags & PixelFormatFourCc) != 0)
        {
            if (fourCc == ComposeFourCc("DXT1"))
            {
                format = "DXT1";
                hasAlpha = true;
            }
            else if (fourCc == ComposeFourCc("DXT3"))
            {
                format = "DXT3";
                hasAlpha = true;
            }
            else if (fourCc == ComposeFourCc("DXT5"))
            {
                format = "DXT5";
                hasAlpha = true;
            }
            else if (fourCc == ComposeFourCc("DX10"))
            {
                dataOffset += 20;
                if (data.Length < dataOffset)
                {
                    throw new MstsFormatException("Truncated DDS DX10 header.");
                }

                var dxgiFormat = BinaryPrimitives.ReadUInt32LittleEndian(data[128..]);
                format = dxgiFormat switch
                {
                    71 => "DXT1",
                    74 => "DXT3",
                    77 => "DXT5",
                    _ => throw new MstsFormatException($"Unsupported DDS DXGI format {dxgiFormat}."),
                };
                hasAlpha = true;
            }
            else
            {
                throw new MstsFormatException($"Unsupported DDS FourCC 0x{fourCc:X8}.");
            }

            var expected = format == "DXT1"
                ? ((width + 3) / 4) * ((height + 3) / 4) * 8
                : ((width + 3) / 4) * ((height + 3) / 4) * 16;
            if (data.Length < dataOffset + expected)
            {
                throw new MstsFormatException("Truncated DDS block data.");
            }

            var pixels = format switch
            {
                "DXT1" => DecodeDxt1(data[dataOffset..], width, height),
                "DXT3" => DecodeDxt3(data[dataOffset..], width, height),
                _ => DecodeDxt5(data[dataOffset..], width, height),
            };

            if (maxDimension > 0 && (width > maxDimension || height > maxDimension))
            {
                pixels = Downsample(pixels, width, height, maxDimension, out width, out height);
            }

            return new DdsImage(width, height, pixels, hasAlpha);
        }

        if ((pixelFlags & PixelFormatRgb) == 0)
        {
            throw new MstsFormatException("Unsupported DDS pixel format flags.");
        }

        var bytesPerPixel = (int)(rgbBitCount / 8);
        if (rgbBitCount is not (24 or 32) || bytesPerPixel is not (3 or 4))
        {
            throw new MstsFormatException($"Unsupported DDS RGB bit count {rgbBitCount}.");
        }

        var sourceStride = width * bytesPerPixel;
        if (data.Length < dataOffset + (sourceStride * height))
        {
            throw new MstsFormatException("Truncated DDS pixel data.");
        }

        hasAlpha = (pixelFlags & PixelFormatAlphaPixels) != 0 && alphaMask != 0;
        var result = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var source = data.Slice(dataOffset + (y * sourceStride), sourceStride);
            var destination = y * width * 4;
            for (var x = 0; x < width; x++)
            {
                var value = bytesPerPixel == 4
                    ? BinaryPrimitives.ReadUInt32LittleEndian(source[(x * 4)..])
                    : (uint)((source[x * 3] << 16) | (source[(x * 3) + 1] << 8) | source[(x * 3) + 2]);

                result[destination++] = ExtractMask(value, redMask, 255);
                result[destination++] = ExtractMask(value, greenMask, 255);
                result[destination++] = ExtractMask(value, blueMask, 255);
                result[destination++] = hasAlpha ? ExtractMask(value, alphaMask, 255) : (byte)255;
            }
        }

        if (maxDimension > 0 && (width > maxDimension || height > maxDimension))
        {
            result = Downsample(result, width, height, maxDimension, out width, out height);
        }

        return new DdsImage(width, height, result, hasAlpha);
    }

    private static uint ComposeFourCc(string value) =>
        (uint)(value[0] | (value[1] << 8) | (value[2] << 16) | (value[3] << 24));

    private static byte ExtractMask(uint value, uint mask, byte fullScale)
    {
        if (mask == 0)
        {
            return fullScale;
        }

        var shift = 0;
        while (((mask >> shift) & 1) == 0)
        {
            shift++;
        }

        var bits = 0;
        var shifted = mask >> shift;
        while ((shifted & 1) != 0)
        {
            bits++;
            shifted >>= 1;
        }

        var component = (value & mask) >> shift;
        var max = (1u << bits) - 1;
        return (byte)((component * fullScale) / max);
    }

    private static byte[] DecodeDxt1(ReadOnlySpan<byte> data, int width, int height) =>
        DecodeColorBlocks(data, width, height, 8, false);

    private static byte[] DecodeDxt3(ReadOnlySpan<byte> data, int width, int height)
    {
        var colors = DecodeColorBlocks(data, width, height, 16, true);
        for (var block = 0; block < data.Length / 16; block++)
        {
            var blockX = (block % ((width + 3) / 4)) * 4;
            var blockY = (block / ((width + 3) / 4)) * 4;
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    var pixelX = blockX + x;
                    var pixelY = blockY + y;
                    if (pixelX >= width || pixelY >= height)
                    {
                        continue;
                    }

                    var nibble = data[(block * 16) + (y * 2) + (x / 2)];
                    var alpha = (x & 1) == 0 ? (nibble & 0x0F) : (nibble >> 4);
                    colors[((pixelY * width) + pixelX) * 4 + 3] = (byte)(alpha * 17);
                }
            }
        }

        return colors;
    }

    private static byte[] DecodeDxt5(ReadOnlySpan<byte> data, int width, int height)
    {
        var colors = DecodeColorBlocks(data, width, height, 16, true);
        for (var block = 0; block < data.Length / 16; block++)
        {
            var alpha0 = data[block * 16];
            var alpha1 = data[(block * 16) + 1];
            var blockX = (block % ((width + 3) / 4)) * 4;
            var blockY = (block / ((width + 3) / 4)) * 4;
            ulong bits = 0;
            for (var i = 0; i < 6; i++)
            {
                bits |= (ulong)data[(block * 16) + 2 + i] << (i * 8);
            }

            var alphas = BuildAlphaPalette(alpha0, alpha1);
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    var pixelX = blockX + x;
                    var pixelY = blockY + y;
                    if (pixelX >= width || pixelY >= height)
                    {
                        continue;
                    }

                    var code = (int)((bits >> ((y * 4) + x) * 3) & 0x7);
                    colors[((pixelY * width) + pixelX) * 4 + 3] = alphas[code];
                }
            }
        }

        return colors;
    }

    private static byte[] BuildAlphaPalette(byte alpha0, byte alpha1)
    {
        var palette = new byte[8];
        palette[0] = alpha0;
        palette[1] = alpha1;
        if (alpha0 > alpha1)
        {
            for (var i = 1; i <= 6; i++)
            {
                palette[i + 1] = (byte)((((7 - i) * alpha0) + (i * alpha1)) / 7);
            }
        }
        else
        {
            for (var i = 1; i <= 4; i++)
            {
                palette[i + 1] = (byte)((((5 - i) * alpha0) + (i * alpha1)) / 5);
            }

            palette[6] = 0;
            palette[7] = 255;
        }

        return palette;
    }

    private static byte[] DecodeColorBlocks(
        ReadOnlySpan<byte> data,
        int width,
        int height,
        int blockSize,
        bool forceFourColor)
    {
        var result = new byte[width * height * 4];
        var blocksX = (width + 3) / 4;

        for (var blockY = 0; blockY < (height + 3) / 4; blockY++)
        {
            for (var blockX = 0; blockX < blocksX; blockX++)
            {
                var offset = ((blockY * blocksX) + blockX) * blockSize;
                var colorOffset = blockSize == 8 ? offset : offset + 8;
                var c0 = BinaryPrimitives.ReadUInt16LittleEndian(data[colorOffset..]);
                var c1 = BinaryPrimitives.ReadUInt16LittleEndian(data[(colorOffset + 2)..]);
                var color0 = From565(c0);
                var color1 = From565(c1);

                byte[] palette;
                if (forceFourColor || c0 > c1)
                {
                    palette = new byte[16];
                    color0.CopyTo(palette, 0);
                    color1.CopyTo(palette, 4);
                    Mix(color0, color1, 2, 1, 3, palette, 8);
                    Mix(color0, color1, 1, 2, 3, palette, 12);
                }
                else
                {
                    palette = new byte[16];
                    color0.CopyTo(palette, 0);
                    color1.CopyTo(palette, 4);
                    Mix(color0, color1, 1, 1, 2, palette, 8);
                    palette[12] = 0;
                    palette[13] = 0;
                    palette[14] = 0;
                    palette[15] = 0;
                }

                var bits = BinaryPrimitives.ReadUInt32LittleEndian(data[(colorOffset + 4)..]);
                for (var y = 0; y < 4; y++)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        var pixelX = (blockX * 4) + x;
                        var pixelY = (blockY * 4) + y;
                        if (pixelX >= width || pixelY >= height)
                        {
                            continue;
                        }

                        var code = (int)((bits >> ((y * 4) + x) * 2) & 0x3);
                        var destination = ((pixelY * width) + pixelX) * 4;
                        result[destination] = palette[(code * 4) + 0];
                        result[destination + 1] = palette[(code * 4) + 1];
                        result[destination + 2] = palette[(code * 4) + 2];
                        result[destination + 3] = palette[(code * 4) + 3];
                    }
                }
            }
        }

        return result;
    }

    private static byte[] From565(ushort value)
    {
        var red = (value >> 11) & 0x1F;
        var green = (value >> 5) & 0x3F;
        var blue = value & 0x1F;
        return new[]
        {
            (byte)((red << 3) | (red >> 2)),
            (byte)((green << 2) | (green >> 4)),
            (byte)((blue << 3) | (blue >> 2)),
            (byte)255,
        };
    }

    private static void Mix(byte[] color0, byte[] color1, int weight0, int weight1, int divisor, byte[] palette, int offset)
    {
        for (var i = 0; i < 3; i++)
        {
            palette[offset + i] =
                (byte)(((color0[i] * weight0) + (color1[i] * weight1)) / divisor);
        }

        palette[offset + 3] = 255;
    }

    private static byte[] Downsample(
        byte[] source,
        int sourceWidth,
        int sourceHeight,
        int maxDimension,
        out int width,
        out int height)
    {
        var scale = (float)maxDimension / MathF.Max(sourceWidth, sourceHeight);
        width = Math.Max(1, (int)MathF.Round(sourceWidth * scale));
        height = Math.Max(1, (int)MathF.Round(sourceHeight * scale));
        var result = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            var sourceY0 = y * sourceHeight / height;
            var sourceY1 = Math.Max(sourceY0 + 1, (y + 1) * sourceHeight / height);
            for (var x = 0; x < width; x++)
            {
                var sourceX0 = x * sourceWidth / width;
                var sourceX1 = Math.Max(sourceX0 + 1, (x + 1) * sourceWidth / width);
                var red = 0;
                var green = 0;
                var blue = 0;
                var alpha = 0;
                var count = 0;

                for (var sy = sourceY0; sy < sourceY1; sy++)
                {
                    for (var sx = sourceX0; sx < sourceX1; sx++)
                    {
                        var index = ((sy * sourceWidth) + sx) * 4;
                        red += source[index];
                        green += source[index + 1];
                        blue += source[index + 2];
                        alpha += source[index + 3];
                        count++;
                    }
                }

                var destination = ((y * width) + x) * 4;
                result[destination] = (byte)(red / count);
                result[destination + 1] = (byte)(green / count);
                result[destination + 2] = (byte)(blue / count);
                result[destination + 3] = (byte)(alpha / count);
            }
        }

        return result;
    }
}
