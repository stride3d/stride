// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Launcher.Views;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class LocalImagesTests
{
    [Theory]
    [InlineData("file:///C:/notes/media/chart.dark.png#gh-dark-mode-only", true)]
    [InlineData("file:///home/user/notes/media/chart.png", true)]
    // MarkView's own loader reads these, and a relative URL is made absolute before it gets here
    [InlineData("https://doc.stride3d.net/en/release-notes/media/4.4/ios.webp", false)]
    [InlineData("data:image/png;base64,AAAA", false)]
    [InlineData("media/chart.png", false)]
    [InlineData("", false)]
    public void OnlyLocalFilesAreLoaded(string url, bool loaded)
        => Assert.Equal(loaded, new LocalImages().CanLoad(url));
}
