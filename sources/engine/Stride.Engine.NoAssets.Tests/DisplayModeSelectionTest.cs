// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;
using Stride.Graphics;

namespace Stride.Engine.Tests;

public class DisplayModeSelectionTest
{
    private static readonly DisplayMode Desktop = new(PixelFormat.B8G8R8A8_UNorm, 2560, 1440, new Rational(60000, 1001));
    private static readonly DisplayMode HighRefresh = new(PixelFormat.B8G8R8A8_UNorm, 2560, 1440, new Rational(144, 1));
    private static readonly DisplayMode Smaller = new(PixelFormat.B8G8R8A8_UNorm, 1920, 1080, new Rational(60, 1));

    [Fact]
    public void UnspecifiedValuesUseDesktopIncludingFractionalRefresh()
    {
        var request = new DisplayMode(PixelFormat.None, 0, 0, default(Rational));
        Assert.Equal(Desktop, GraphicsOutput.SelectDisplayMode([Smaller, HighRefresh, Desktop], Desktop, request));
    }

    [Fact]
    public void RequestedRefreshDistinguishesFractionalAndIntegerRates()
    {
        var integer = Desktop with { RefreshRate = new Rational(60, 1) };
        Assert.Equal(Desktop, GraphicsOutput.SelectDisplayMode([integer, Desktop], integer, Desktop));
        Assert.Equal(integer, GraphicsOutput.SelectDisplayMode([Desktop, integer], Desktop, integer));
    }

    [Fact]
    public void RequestedSizeTakesPriorityOverRefresh()
    {
        var request = Smaller with { Width = 1900, Height = 1060, RefreshRate = new Rational(144, 1) };
        Assert.Equal(Smaller, GraphicsOutput.SelectDisplayMode([Desktop, HighRefresh, Smaller], Desktop, request));
    }

    [Fact]
    public void SrgbRequestUsesTheCorrespondingDisplayFormat()
    {
        var request = Desktop with { Format = PixelFormat.B8G8R8A8_UNorm_SRgb };
        var otherFormat = Desktop with { Format = PixelFormat.R8G8B8A8_UNorm };
        Assert.Equal(Desktop, GraphicsOutput.SelectDisplayMode([otherFormat, Desktop], Desktop, request));
    }

    [Fact]
    public void UnknownRefreshDoesNotCauseDivisionByZero()
    {
        var unknown = Smaller with { RefreshRate = default };
        Assert.Equal(unknown, GraphicsOutput.SelectDisplayMode([unknown], null, unknown));
    }

    [Fact]
    public void MissingModesKeepTheRequest()
    {
        Assert.Equal(Smaller, GraphicsOutput.SelectDisplayMode([], null, Smaller));
        Assert.Equal(Smaller, GraphicsOutput.SelectDisplayMode([], Desktop, Smaller));
    }

    [Fact]
    public void MissingModesFillUnspecifiedValuesFromDesktop()
    {
        var request = new DisplayMode(PixelFormat.R10G10B10A2_UNorm, 0, 0, default(Rational));
        Assert.Equal(Desktop with { Format = PixelFormat.R10G10B10A2_UNorm },
            GraphicsOutput.SelectDisplayMode([], Desktop, request));
    }

    [Fact]
    public void PositiveRefreshWithZeroDenominatorIsRejected()
    {
        var request = Desktop with { RefreshRate = new Rational(60, 0) };
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphicsOutput.SelectDisplayMode([Desktop], Desktop, request));
    }

    [Theory]
    [InlineData(-1, 1080)]
    [InlineData(1920, -1)]
    public void NegativeDimensionsAreRejected(int width, int height)
    {
        var request = Desktop with { Width = width, Height = height };
        Assert.Throws<ArgumentOutOfRangeException>(() => GraphicsOutput.SelectDisplayMode([Desktop], Desktop, request));
    }
}
