// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Launcher.ViewModels;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class ThemeImagesTests
{
    private const string Both = "Compile time:\n\n![Chart](media/chart.light.webp#gh-light-mode-only)![Chart](media/chart.dark.webp#gh-dark-mode-only)\n\nAfter.\n";

    [Fact]
    public void TheDarkThemeKeepsTheDarkImage()
        => Assert.Equal("Compile time:\n\n![Chart](media/chart.dark.webp#gh-dark-mode-only)\n\nAfter.\n", ThemeImages.Keep(Both, dark: true));

    [Fact]
    public void TheLightThemeKeepsTheLightImage()
        => Assert.Equal("Compile time:\n\n![Chart](media/chart.light.webp#gh-light-mode-only)\n\nAfter.\n", ThemeImages.Keep(Both, dark: false));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AnImageForEveryThemeAndTheTextStay(bool dark)
    {
        const string markdown = "# 4.5\n\n![Logo](media/logo.png) and [a link](page.md#gh-dark-mode-only), `![code](a.png#gh-dark-mode-only)`.\n";
        Assert.Same(markdown, ThemeImages.Keep(markdown, dark));
    }

    [Fact]
    public void ALinkAroundOnlyTheImageGoesWithIt()
    {
        const string markdown = "[![Chart](chart.light.webp#gh-light-mode-only)](https://stride3d.net) [![Chart](chart.dark.webp#gh-dark-mode-only)](https://stride3d.net)";
        Assert.Equal(" [![Chart](chart.dark.webp#gh-dark-mode-only)](https://stride3d.net)", ThemeImages.Keep(markdown, dark: true));
    }

    [Fact]
    public void ALinkWithTextAroundTheImageStays()
        => Assert.Equal("[See ](https://stride3d.net)", ThemeImages.Keep("[See ![Chart](chart.light.webp#gh-light-mode-only)](https://stride3d.net)", dark: true));

    [Fact]
    public void AReferenceImageIsKeptByItsUrl()
    {
        const string markdown = "![Chart][light]\n\n[light]: chart.light.webp#gh-light-mode-only\n";
        Assert.Equal("\n\n[light]: chart.light.webp#gh-light-mode-only\n", ThemeImages.Keep(markdown, dark: true));
    }

    [Fact]
    public void WindowsLineEndingsAreKept()
        => Assert.Equal("A\r\n\r\n![Chart](d.webp#gh-dark-mode-only)\r\n\r\nB\r\n", ThemeImages.Keep("A\r\n\r\n![Chart](l.webp#gh-light-mode-only)![Chart](d.webp#gh-dark-mode-only)\r\n\r\nB\r\n", dark: true));

    [Fact]
    public void TheImageOfAConvertedVideoStays()
    {
        // What ReleaseNotesViewModel makes of a <video> tag before the theme is applied
        const string markdown = "![](media/video-poster.jpg)\r\n\r\n[_Click to watch the video_](media/video.mp4)";
        Assert.Same(markdown, ThemeImages.Keep(markdown, dark: false));
    }
}
