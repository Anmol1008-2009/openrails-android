using System.Buffers.Binary;
using OpenRails.Core.Formats;
using Xunit;

namespace OpenRails.Core.Tests;

public class DdsImageTests
{
    [Fact]
    public void Decodes_dxt1_solid_red_block()
    {
        var data = BuildDds(4, 4, "DXT1", Dxt1Block(0xF800, 0x001F, 0));
        var image = DdsImage.Decode(data);

        Assert.Equal(4, image.Width);
        Assert.Equal(4, image.Height);
        Assert.Equal(255, image.Pixels[0]);
        Assert.Equal(0, image.Pixels[1]);
        Assert.Equal(0, image.Pixels[2]);
        Assert.Equal(255, image.Pixels[3]);
    }

    [Fact]
    public void Decodes_dxt1_three_color_mode()
    {
        var data = BuildDds(4, 4, "DXT1", Dxt1Block(0x001F, 0xF800, 0));
        var image = DdsImage.Decode(data);

        Assert.Equal(0, image.Pixels[0]);
        Assert.Equal(0, image.Pixels[1]);
        Assert.Equal(255, image.Pixels[2]);
    }

    [Fact]
    public void Decodes_dxt3_alpha_nibbles()
    {
        var block = new byte[16];
        block[0] = 0x0F;
        for (var i = 2; i < 16; i++)
        {
            block[i] = 0;
        }

        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(8), 0xF800);
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(10), 0x001F);
        var data = BuildDds(4, 4, "DXT3", block);
        var image = DdsImage.Decode(data);

        Assert.Equal(255, image.Pixels[3]);
        Assert.Equal(0, image.Pixels[7]);
    }

    [Fact]
    public void Decodes_dxt5_alpha_palette()
    {
        var block = new byte[16];
        block[0] = 255;
        block[1] = 0;
        for (var i = 2; i < 16; i++)
        {
            block[i] = 0;
        }

        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(8), 0xF800);
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(10), 0x001F);
        var data = BuildDds(4, 4, "DXT5", block);
        var image = DdsImage.Decode(data);

        Assert.Equal(255, image.Pixels[3]);
        Assert.Equal(255, image.Pixels[7]);
    }

    [Fact]
    public void Downsamples_to_requested_maximum_dimension()
    {
        var block = Dxt1Block(0xF800, 0x001F, 0);
        var data = BuildDds(8, 4, "DXT1", new[] { block, block }.SelectMany(value => value).ToArray());
        var image = DdsImage.Decode(data, maxDimension: 4);

        Assert.Equal(4, image.Width);
        Assert.Equal(2, image.Height);
        Assert.Equal(4 * 2 * 4, image.Pixels.Length);
    }

    [Fact]
    public void Decodes_uncompressed_32_bit_rgba()
    {
        var data = BuildUncompressedDds(2, 1, new byte[]
        {
            0x20, 0x30, 0x40, 0x80,
            0x10, 0x20, 0x30, 0xFF,
        });
        var image = DdsImage.Decode(data);

        Assert.Equal(0x20, image.Pixels[0]);
        Assert.Equal(0x30, image.Pixels[1]);
        Assert.Equal(0x40, image.Pixels[2]);
        Assert.Equal(0x80, image.Pixels[3]);
        Assert.True(image.HasAlpha);
    }

    [Fact]
    public void Rejects_non_dds_data()
    {
        Assert.Throws<OpenRails.Core.Msts.MstsFormatException>(() =>
            DdsImage.Decode(new byte[128]));
    }

    private static byte[] Dxt1Block(ushort color0, ushort color1, uint indices)
    {
        var block = new byte[8];
        BinaryPrimitives.WriteUInt16LittleEndian(block, color0);
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2), color1);
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), indices);
        return block;
    }

    private static byte[] BuildDds(int width, int height, string fourCc, byte[] pixels)
    {
        var data = new byte[128 + pixels.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(data, 0x20534444);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), 124);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), 0x1 | 0x2 | 0x4 | 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12), (uint)height);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16), (uint)width);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(76), 32);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(80), 0x4);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(84),
            (uint)(fourCc[0] | (fourCc[1] << 8) | (fourCc[2] << 16) | (fourCc[3] << 24)));
        pixels.CopyTo(data, 128);
        return data;
    }

    private static byte[] BuildUncompressedDds(int width, int height, byte[] pixels)
    {
        var data = new byte[128 + pixels.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(data, 0x20534444);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), 124);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), 0x1 | 0x2 | 0x4 | 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12), (uint)height);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16), (uint)width);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20), (uint)(width * 4));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(76), 32);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(80), 0x1 | 0x40);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(88), 32);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(92), 0x000000FF);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(96), 0x0000FF00);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(100), 0x00FF0000);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(104), 0xFF000000);
        pixels.CopyTo(data, 128);
        return data;
    }
}
