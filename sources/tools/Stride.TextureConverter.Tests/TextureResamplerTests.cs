// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Graphics;
using Stride.TextureConverter.Requests;
using Stride.TextureConverter.TexLibraries;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Stride.TextureConverter.Tests;

public class TextureResamplerTests
{
    [Theory]
    [InlineData(Filter.Rescaling.Box)]
    [InlineData(Filter.Rescaling.Bilinear)]
    [InlineData(Filter.Rescaling.Bicubic)]
    [InlineData(Filter.Rescaling.BSpline)]
    [InlineData(Filter.Rescaling.CatmullRom)]
    [InlineData(Filter.Rescaling.Lanczos3)]
    public void MatchesExistingOpaqueResize(Filter.Rescaling filter)
    {
        var pixels = new byte[5 * 4 * 4];
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 5; x++)
        {
            int offset = (y * 5 + x) * 4;
            pixels[offset] = (byte)(x * 17 + y * 23);
            pixels[offset + 1] = (byte)(y * 51);
            pixels[offset + 2] = (byte)(x * 41);
            pixels[offset + 3] = 255;
        }
        foreach (var (width, height) in new[] { (3, 2), (9, 7) })
        {
            var expectedBytes = GetResizeReference("opaque", filter, width, height);
            var actual = TextureResampler.Resize(pixels, 5, 4, width, height, filter);
            Assert.Equal(expectedBytes.Length, actual.Length);
            for (int i = 0; i < actual.Length; i++)
                Assert.InRange(Math.Abs(actual[i] - expectedBytes[i]), 0, 1);
        }
    }

    [Theory]
    [InlineData(".webp")]
    [InlineData(".qoi")]
    [InlineData(".pbm")]
    [InlineData(".tif")]
    [InlineData(".tiff")]
    public void UnsupportedFormatsAreNotClaimed(string extension)
    {
        using var library = new StbImageNextLib();
        Assert.False(library.CanHandleRequest(PixelFormat.B8G8R8A8_UNorm, new FileLoadingRequest("texture" + extension, false)));
        Assert.False(library.CanHandleRequest(PixelFormat.B8G8R8A8_UNorm, new ExportRequest("texture" + extension, 0)));
    }

    [Fact]
    public void GifCanBeLoadedButNotExported()
    {
        using var library = new StbImageNextLib();
        Assert.True(library.CanHandleRequest(PixelFormat.B8G8R8A8_UNorm, new FileLoadingRequest("texture.GIF", false)));
        Assert.False(library.CanHandleRequest(PixelFormat.B8G8R8A8_UNorm, new ExportRequest("texture.gif", 0)));
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".bmp")]
    [InlineData(".tga")]
    public void BackendRoundTripsImageFormats(string extension)
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"stride-codec-{Guid.NewGuid():N}.png");
        var outputPath = Path.ChangeExtension(sourcePath, extension == ".png" ? ".out.png" : extension);
        try
        {
            using (var source = Image.New2D(1, 1, 1, PixelFormat.R8G8B8A8_UNorm))
            {
                source.PixelBuffer[0].SetPixel(0, 0, 0xFF3C2814u);
                using var stream = File.Create(sourcePath);
                source.Save(stream, ImageFileType.Png);
            }
            using var library = new StbImageNextLib();
            using var image = new TexImage();
            library.Execute(image, new FileLoadingRequest(sourcePath, false));
            library.Execute(image, new ExportRequest(outputPath, 0));
            using var loaded = new TexImage();
            library.Execute(loaded, new FileLoadingRequest(outputPath, false));
            var actual = new byte[4];
            Marshal.Copy(loaded.Data, actual, 0, actual.Length);
            Assert.Equal(new byte[] { 60, 40, 20, 255 }, actual);
        }
        finally
        {
            File.Delete(sourcePath);
            File.Delete(outputPath);
        }
    }

    [Fact]
    public void BackendUsesResamplerAndMaterializesPixels()
    {
        var path = Path.Combine(Path.GetTempPath(), $"stride-resize-{Guid.NewGuid():N}.png");
        try
        {
            using (var source = Image.New2D(2, 2, 1, PixelFormat.R8G8B8A8_UNorm))
            {
                source.PixelBuffer[0].SetPixel(0, 0, 0x803C2814u);
                source.PixelBuffer[0].SetPixel(1, 0, 0xFF786450u);
                source.PixelBuffer[0].SetPixel(0, 1, 0xC8968C82u);
                source.PixelBuffer[0].SetPixel(1, 1, 0u);
                using var stream = File.Create(path);
                source.Save(stream, ImageFileType.Png);
            }
            using var library = new StbImageNextLib();
            using var image = new TexImage();
            library.Execute(image, new FileLoadingRequest(path, false));
            var sourceBytes = new byte[16];
            Marshal.Copy(image.Data, sourceBytes, 0, sourceBytes.Length);
            library.Execute(image, new FixedRescalingRequest(3, 3, Filter.Rescaling.Bilinear));
            library.EndLibrary(image);
            var actual = new byte[36];
            Marshal.Copy(image.Data, actual, 0, actual.Length);
            Assert.Equal(TextureResampler.Resize(sourceBytes, 2, 2, 3, 3, Filter.Rescaling.Bilinear), actual);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TransparentPixelsDoNotBleedIntoOpaquePixels()
    {
        byte[] source = [255, 0, 0, 0, 0, 0, 255, 255];
        var result = TextureResampler.Resize(source, 2, 1, 1, 1, Filter.Rescaling.Bilinear);
        Assert.Equal(new byte[] { 0, 0, 255, 128 }, result);
    }

    [Fact]
    public void DownscaleAcrossMoreThanCachedRows()
    {
        var source = new byte[2 * 32 * 4];
        for (int i = 0; i < source.Length; i += 4)
        {
            source[i] = 60;
            source[i + 1] = 120;
            source[i + 2] = 180;
            source[i + 3] = 255;
        }

        var result = TextureResampler.Resize(source, 2, 32, 1, 2, Filter.Rescaling.Lanczos3);
        Assert.Equal(new byte[] { 60, 120, 180, 255, 60, 120, 180, 255 }, result);
    }

    [Fact]
    public void ResizeUsesBoundedIntermediateMemory()
    {
        var source = new byte[256 * 512 * 4];
        TextureResampler.Resize(source, 256, 512, 255, 511, Filter.Rescaling.Bilinear);

        long before = GC.GetAllocatedBytesForCurrentThread();
        var result = TextureResampler.Resize(source, 256, 512, 255, 511, Filter.Rescaling.Bilinear);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(255 * 511 * 4, result.Length);
        Assert.InRange(allocated, 0, 2_000_000);
    }

    [Theory]
    [InlineData(Filter.Rescaling.Box)]
    [InlineData(Filter.Rescaling.Bilinear)]
    [InlineData(Filter.Rescaling.Bicubic)]
    [InlineData(Filter.Rescaling.BSpline)]
    [InlineData(Filter.Rescaling.CatmullRom)]
    [InlineData(Filter.Rescaling.Lanczos3)]
    public void MatchesExistingTransparentResize(Filter.Rescaling filter)
    {
        var pixels = new byte[4 * 3 * 4];
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 4; x++)
        {
            int offset = (y * 4 + x) * 4;
            pixels[offset] = (byte)(x * 31 + y * 14);
            pixels[offset + 1] = (byte)(y * 71);
            pixels[offset + 2] = (byte)(x * 52);
            pixels[offset + 3] = (byte)(32 + x * 39 + y * 24);
        }
        var expectedBytes = GetResizeReference("transparent", filter, 7, 5);
        var actual = TextureResampler.Resize(pixels, 4, 3, 7, 5, filter);
        Assert.Equal(expectedBytes.Length, actual.Length);
        for (int i = 0; i < actual.Length; i++)
            Assert.InRange(int.Abs(actual[i] - expectedBytes[i]), 0, 1);
    }

    private static byte[] GetResizeReference(string kind, Filter.Rescaling filter, int width, int height)
    {
        // BGRA pixels captured from the previous resize implementation.
        ReadOnlySpan<uint> pixels = (kind, filter, width, height) switch
        {
            ("opaque", Filter.Rescaling.Bilinear, 3, 2) => [0xFF122418, 0xFF522432, 0xFF92244D, 0xFF12753C, 0xFF527557, 0xFF927571],
            ("opaque", Filter.Rescaling.Bicubic or Filter.Rescaling.CatmullRom, 3, 2) => [0xFF0F1E14, 0xFF521E2F, 0xFF951E4B, 0xFF0F7B3E, 0xFF527B5A, 0xFF957B75],
            ("opaque", Filter.Rescaling.Box, 3, 2) => [0xFF151A14, 0xFF521A2E, 0xFF901A47, 0xFF158042, 0xFF52805C, 0xFF908075],
            ("opaque", Filter.Rescaling.BSpline, 3, 2) => [0xFF1C2E20, 0xFF522E37, 0xFF882E4D, 0xFF1C6B3C, 0xFF526B52, 0xFF886B69],
            ("opaque", Filter.Rescaling.Lanczos3, 3, 2) => [0xFF0D1B12, 0xFF521B2E, 0xFF971B4B, 0xFF0D7E3E, 0xFF527E5B, 0xFF977E77],
            ("opaque", Filter.Rescaling.Bilinear, 9, 7) => [0xFF000000, 0xFF0E0006, 0xFF24000F, 0xFF3B0019, 0xFF520022, 0xFF69002B, 0xFF800035, 0xFF96003E, 0xFFA40044, 0xFF001208, 0xFF0E120E, 0xFF241217, 0xFF3B1221, 0xFF52122A, 0xFF691234, 0xFF80123D, 0xFF961247, 0xFFA4124C, 0xFF002F15, 0xFF0E2F1B, 0xFF242F24, 0xFF3B2F2E, 0xFF522F37, 0xFF692F41, 0xFF802F4A, 0xFF962F54, 0xFFA42F59, 0xFF004D23, 0xFF0E4D28, 0xFF244D32, 0xFF3B4D3B, 0xFF524D45, 0xFF694D4E, 0xFF804D57, 0xFF964D61, 0xFFA44D67, 0xFF006A30, 0xFF0E6A35, 0xFF246A3F, 0xFF3B6A48, 0xFF526A52, 0xFF696A5B, 0xFF806A65, 0xFF966A6E, 0xFFA46A74, 0xFF00873D, 0xFF0E8742, 0xFF24874C, 0xFF3B8755, 0xFF52875F, 0xFF698768, 0xFF808772, 0xFF96877B, 0xFFA48781, 0xFF009945, 0xFF0E994B, 0xFF249954, 0xFF3B995E, 0xFF529967, 0xFF699970, 0xFF80997A, 0xFF969983, 0xFFA49989],
            ("opaque", Filter.Rescaling.Bicubic or Filter.Rescaling.CatmullRom, 9, 7) => [0xFF000000, 0xFF0A0002, 0xFF24000D, 0xFF3B0017, 0xFF520020, 0xFF69002A, 0xFF800033, 0xFF9A003E, 0xFFA70044, 0xFF000D05, 0xFF0A0D0A, 0xFF240D15, 0xFF3B0D1F, 0xFF520D28, 0xFF690D32, 0xFF800D3B, 0xFF9A0D46, 0xFFA70D4B, 0xFF002F14, 0xFF0A2F19, 0xFF242F24, 0xFF3B2F2E, 0xFF522F37, 0xFF692F41, 0xFF802F4A, 0xFF9A2F55, 0xFFA72F5B, 0xFF004D21, 0xFF0A4D27, 0xFF244D31, 0xFF3B4D3B, 0xFF524D45, 0xFF694D4E, 0xFF804D58, 0xFF9A4D62, 0xFFA74D68, 0xFF006A2E, 0xFF0A6A34, 0xFF246A3F, 0xFF3B6A48, 0xFF526A52, 0xFF696A5B, 0xFF806A65, 0xFF9A6A70, 0xFFA76A75, 0xFF008C3E, 0xFF0A8C43, 0xFF248C4E, 0xFF3B8C57, 0xFF528C61, 0xFF698C6A, 0xFF808C74, 0xFF9A8C7F, 0xFFA78C84, 0xFF009D45, 0xFF0A9D4B, 0xFF249D56, 0xFF3B9D5F, 0xFF529D69, 0xFF699D72, 0xFF809D7C, 0xFF9A9D87, 0xFFA79D8C],
            ("opaque", Filter.Rescaling.Box, 9, 7) => [0xFF000000, 0xFF000000, 0xFF290011, 0xFF290011, 0xFF520022, 0xFF7B0033, 0xFF7B0033, 0xFFA40044, 0xFFA40044, 0xFF000000, 0xFF000000, 0xFF290011, 0xFF290011, 0xFF520022, 0xFF7B0033, 0xFF7B0033, 0xFFA40044, 0xFFA40044, 0xFF003317, 0xFF003317, 0xFF293328, 0xFF293328, 0xFF523339, 0xFF7B334A, 0xFF7B334A, 0xFFA4335B, 0xFFA4335B, 0xFF00662E, 0xFF00662E, 0xFF29663F, 0xFF29663F, 0xFF526650, 0xFF7B6661, 0xFF7B6661, 0xFFA46672, 0xFFA46672, 0xFF00662E, 0xFF00662E, 0xFF29663F, 0xFF29663F, 0xFF526650, 0xFF7B6661, 0xFF7B6661, 0xFFA46672, 0xFFA46672, 0xFF009945, 0xFF009945, 0xFF299956, 0xFF299956, 0xFF529967, 0xFF7B9978, 0xFF7B9978, 0xFFA49989, 0xFFA49989, 0xFF009945, 0xFF009945, 0xFF299956, 0xFF299956, 0xFF529967, 0xFF7B9978, 0xFF7B9978, 0xFFA49989, 0xFFA49989],
            ("opaque", Filter.Rescaling.BSpline, 9, 7) => [0xFF050605, 0xFF110609, 0xFF240612, 0xFF3B061B, 0xFF520625, 0xFF69062E, 0xFF800638, 0xFF930640, 0xFF9F0645, 0xFF05150C, 0xFF111511, 0xFF241519, 0xFF3B1522, 0xFF52152C, 0xFF691535, 0xFF80153F, 0xFF931547, 0xFF9F154C, 0xFF052F17, 0xFF112F1C, 0xFF242F24, 0xFF3B2F2E, 0xFF522F37, 0xFF692F41, 0xFF802F4A, 0xFF932F53, 0xFF9F2F57, 0xFF054D24, 0xFF114D29, 0xFF244D32, 0xFF3B4C3B, 0xFF524D45, 0xFF694C4E, 0xFF804D57, 0xFF934D60, 0xFF9F4D65, 0xFF056A32, 0xFF116A36, 0xFF246A3F, 0xFF3B6A48, 0xFF526A52, 0xFF696A5B, 0xFF806A65, 0xFF936A6D, 0xFF9F6A72, 0xFF05843D, 0xFF118442, 0xFF24844A, 0xFF3B8454, 0xFF52845D, 0xFF698467, 0xFF808470, 0xFF938478, 0xFF9F847D, 0xFF059344, 0xFF119349, 0xFF249351, 0xFF3B935B, 0xFF529364, 0xFF69936E, 0xFF809377, 0xFF939380, 0xFF9F9384],
            ("opaque", Filter.Rescaling.Lanczos3, 9, 7) => [0xFF000000, 0xFF090002, 0xFF23000D, 0xFF3E0018, 0xFF520020, 0xFF660029, 0xFF810034, 0xFF9B003F, 0xFFA70044, 0xFF000C04, 0xFF090C09, 0xFF230C14, 0xFF3E0C1F, 0xFF520C27, 0xFF660C30, 0xFF810C3B, 0xFF9B0C46, 0xFFA70C4B, 0xFF002E14, 0xFF092E18, 0xFF232E24, 0xFF3E2E2F, 0xFF522E37, 0xFF662E3F, 0xFF812E4A, 0xFF9B2E55, 0xFFA72E5A, 0xFF004D21, 0xFF094C26, 0xFF234C31, 0xFF3E4D3C, 0xFF524D45, 0xFF664D4D, 0xFF814C58, 0xFF9B4C63, 0xFFA74D68, 0xFF006B2F, 0xFF096B34, 0xFF236B3F, 0xFF3E6B4A, 0xFF526B52, 0xFF666B5A, 0xFF816B65, 0xFF9B6B71, 0xFFA76B75, 0xFF008D3E, 0xFF098D43, 0xFF238D4E, 0xFF3E8D59, 0xFF528D62, 0xFF668D6A, 0xFF818D75, 0xFF9B8D80, 0xFFA78D85, 0xFF009D45, 0xFF099D4A, 0xFF239D55, 0xFF3E9D60, 0xFF529D69, 0xFF669D71, 0xFF819D7C, 0xFF9B9D87, 0xFFA79D8C],
            ("transparent", Filter.Rescaling.Bicubic or Filter.Rescaling.CatmullRom, 7, 5) => [0x1B000000, 0x2814000A, 0x4230001B, 0x594E002D, 0x6F6C003F, 0x89900055, 0x969F005E, 0x24001B00, 0x32131A10, 0x4B301822, 0x624E1833, 0x786C1745, 0x9290175A, 0x9F9F1763, 0x3500470A, 0x42114718, 0x5C30472B, 0x734E473C, 0x896C474E, 0xA3904764, 0xB09F476D, 0x46007B14, 0x53117B22, 0x6D307A35, 0x834E7A47, 0x9A6C7A58, 0xB390796E, 0xC19F7977, 0x4F009219, 0x5C109226, 0x7630923A, 0x8C4E924B, 0xA36C935D, 0xBD909372, 0xCA9F937C],
            ("transparent", Filter.Rescaling.BSpline, 7, 5) => [0x270B0D09, 0x33200C15, 0x473A0B25, 0x5D550A35, 0x74720A46, 0x878A0A54, 0x93970A5C, 0x2F0A2A0E, 0x3B1F281A, 0x4F392629, 0x6555253A, 0x7C71244B, 0x8F8A2359, 0x9B972361, 0x3C095015, 0x481D4F21, 0x5C384D30, 0x73544C41, 0x89714B52, 0x9D8A4B61, 0xA9974A69, 0x4A09741C, 0x561C7327, 0x69377237, 0x80537148, 0x96707159, 0xAA897168, 0xB6977070, 0x52088820, 0x5E1B872B, 0x7136873B, 0x8853874C, 0x9E70875D, 0xB289876C, 0xBE978675],
            ("transparent", Filter.Rescaling.Bilinear, 7, 5) => [0x20000000, 0x2E1D0011, 0x4432001E, 0x5B540032, 0x716D0041, 0x878D0054, 0x959C005D, 0x2A002608, 0x381B2417, 0x4E322224, 0x64532038, 0x7A6D2047, 0x918D1F5A, 0x9F9C1F63, 0x3800470E, 0x4619471D, 0x5C32472C, 0x7352473F, 0x896D474F, 0x9F8C4762, 0xAD9C476B, 0x46007718, 0x54187626, 0x6B327535, 0x81527548, 0x976D7458, 0xAD8C746A, 0xBB9C7474, 0x50008E1C, 0x5E188E2A, 0x74318E39, 0x8B528E4D, 0xA16D8E5D, 0xB78C8E6F, 0xC59C8E79],
            ("transparent", Filter.Rescaling.Box, 7, 5) => [0x20000000, 0x20000000, 0x4734001F, 0x6E68003E, 0x6E68003E, 0x959C005D, 0x959C005D, 0x20000000, 0x20000000, 0x4734001F, 0x6E68003E, 0x6E68003E, 0x959C005D, 0x959C005D, 0x3800470E, 0x3800470E, 0x5F34472D, 0x8668474C, 0x8668474C, 0xAD9C476B, 0xAD9C476B, 0x50008E1C, 0x50008E1C, 0x77348E3B, 0x9E688E5A, 0x9E688E5A, 0xC59C8E79, 0xC59C8E79, 0x50008E1C, 0x50008E1C, 0x77348E3B, 0x9E688E5A, 0x9E688E5A, 0xC59C8E79, 0xC59C8E79],
            ("transparent", Filter.Rescaling.Lanczos3, 7, 5) => [0x1B000000, 0x27100008, 0x4231001C, 0x59470029, 0x706E0040, 0x8A910055, 0x96A0005E, 0x22000F00, 0x2E0F0F0C, 0x49310F20, 0x6047102E, 0x776E1044, 0x9191105A, 0x9EA01062, 0x3500470B, 0x410E4716, 0x5B31472B, 0x73484739, 0x8A6E474F, 0xA4914764, 0xB0A0476D, 0x47007D16, 0x540E7D21, 0x6E307D36, 0x85497E44, 0x9C6D7E5A, 0xB7917E6F, 0xC3A07E78, 0x4F00931A, 0x5B0E9325, 0x7530933A, 0x8C499349, 0xA36D935E, 0xBE919373, 0xCAA0937C],
            _ => throw new InvalidOperationException($"Missing resize reference: {kind} {filter} {width} {height}"),
        };
        var bytes = new byte[pixels.Length * sizeof(uint)];
        for (int i = 0; i < pixels.Length; i++)
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * sizeof(uint)), pixels[i]);
        return bytes;
    }
}
