// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestColorExtensions
{
    [Fact]
    public void TestCanConvertStringToRgba_ValidHexShort()
    {
        Assert.True(ColorExtensions.CanConvertStringToRgba("#FFF"));
    }

    [Fact]
    public void TestCanConvertStringToRgba_ValidHexMedium()
    {
        Assert.True(ColorExtensions.CanConvertStringToRgba("#FFFFFF"));
    }

    [Fact]
    public void TestCanConvertStringToRgba_ValidHexLong()
    {
        Assert.True(ColorExtensions.CanConvertStringToRgba("#FFFFFFFF"));
    }

    [Fact]
    public void TestCanConvertStringToRgba_InvalidNoHash()
    {
        Assert.False(ColorExtensions.CanConvertStringToRgba("FFFFFF"));
    }

    [Fact]
    public void TestCanConvertStringToRgba_Null()
    {
        Assert.False(ColorExtensions.CanConvertStringToRgba(null));
    }

    [Fact]
    public void TestStringToRgba_ShortFormat()
    {
        // #FFF should expand to #FFFFFFFF (white with full alpha)
        var result = ColorExtensions.StringToRgba("#FFF");
        Assert.Equal(0xFFFFFFFFu, result);
    }

    [Fact]
    public void TestStringToRgba_ShortFormatBlack()
    {
        // #000 should expand to #FF000000 (black with full alpha)
        var result = ColorExtensions.StringToRgba("#000");
        Assert.Equal(0xFF000000u, result);
    }

    [Fact]
    public void TestStringToRgba_MediumFormat()
    {
        // #FF0000 should become RGBA: 0xFF0000FF (red with full alpha)
        var result = ColorExtensions.StringToRgba("#FF0000");
        Assert.Equal(0xFF0000FFu, result);
    }

    [Fact]
    public void TestStringToRgba_MediumFormatGreen()
    {
        // #00FF00 should become RGBA: 0xFF00FF00 (green with full alpha)
        var result = ColorExtensions.StringToRgba("#00FF00");
        Assert.Equal(0xFF00FF00u, result);
    }

    [Fact]
    public void TestStringToRgba_MediumFormatBlue()
    {
        // #0000FF should become RGBA: 0xFFFF0000 (blue with full alpha)
        var result = ColorExtensions.StringToRgba("#0000FF");
        Assert.Equal(0xFFFF0000u, result);
    }

    [Fact]
    public void TestStringToRgba_LongFormat()
    {
        // #80FF0000 should become RGBA: 0x800000FF (red with half alpha)
        var result = ColorExtensions.StringToRgba("#80FF0000");
        Assert.Equal(0x800000FFu, result);
    }

    [Fact]
    public void TestStringToRgba_LongFormatFullAlpha()
    {
        // #FFFFFFFF should stay white with full alpha
        var result = ColorExtensions.StringToRgba("#FFFFFFFF");
        Assert.Equal(0xFFFFFFFFu, result);
    }

    [Fact]
    public void TestStringToRgba_InvalidFormat()
    {
        // Invalid format should return default (0xFF000000)
        var result = ColorExtensions.StringToRgba("NotAColor");
        Assert.Equal(0xFF000000u, result);
    }

    [Fact]
    public void TestStringToRgba_Null()
    {
        // Null should return default (0xFF000000)
        var result = ColorExtensions.StringToRgba(null);
        Assert.Equal(0xFF000000u, result);
    }

    [Fact]
    public void TestRgbToString_Red()
    {
        // RGB red: R=255, G=0, B=0 stored as 0x0000FF
        var result = ColorExtensions.RgbToString(0x0000FF);
        Assert.Equal("#FF0000", result);
    }

    [Fact]
    public void TestRgbToString_Green()
    {
        // RGB green: R=0, G=255, B=0 stored as 0x00FF00
        var result = ColorExtensions.RgbToString(0x00FF00);
        Assert.Equal("#00FF00", result);
    }

    [Fact]
    public void TestRgbToString_Blue()
    {
        // RGB blue: R=0, G=0, B=255 stored as 0xFF0000
        var result = ColorExtensions.RgbToString(0xFF0000);
        Assert.Equal("#0000FF", result);
    }

    [Fact]
    public void TestRgbToString_White()
    {
        var result = ColorExtensions.RgbToString(0xFFFFFF);
        Assert.Equal("#FFFFFF", result);
    }

    [Fact]
    public void TestRgbToString_Black()
    {
        var result = ColorExtensions.RgbToString(0x000000);
        Assert.Equal("#000000", result);
    }

    [Fact]
    public void TestRgbaToString_RedFullAlpha()
    {
        // RGBA red with full alpha: A=255, R=255, G=0, B=0
        var result = ColorExtensions.RgbaToString(unchecked((int)0xFF0000FF));
        Assert.Equal("#FFFF0000", result);
    }

    [Fact]
    public void TestRgbaToString_GreenHalfAlpha()
    {
        // RGBA green with half alpha: A=128, R=0, G=255, B=0
        var result = ColorExtensions.RgbaToString(unchecked((int)0x8000FF00));
        Assert.Equal("#8000FF00", result);
    }

    [Fact]
    public void TestRgbaToString_BlueNoAlpha()
    {
        // RGBA blue with no alpha: A=0, R=0, G=0, B=255
        var result = ColorExtensions.RgbaToString(0x00FF0000);
        Assert.Equal("#000000FF", result);
    }

    [Fact]
    public void TestRgbaToString_WhiteFullAlpha()
    {
        var result = ColorExtensions.RgbaToString(unchecked((int)0xFFFFFFFF));
        Assert.Equal("#FFFFFFFF", result);
    }

    [Fact]
    public void TestRgbaToString_BlackNoAlpha()
    {
        var result = ColorExtensions.RgbaToString(0x00000000);
        Assert.Equal("#00000000", result);
    }
}
