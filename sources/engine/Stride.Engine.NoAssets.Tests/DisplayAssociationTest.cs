// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Engine.Tests;

public class DisplayAssociationTest
{
    private static readonly DisplayMode Mode = new(PixelFormat.B8G8R8A8_UNorm, 1920, 1080, 60u);

    [Fact]
    public void IdenticalNamesAndBoundsDoNotEstablishGpuOwnership()
    {
        var first = Display(1);
        var second = Display(2);
        var attached = DisplayEnumeration.ForAdapter([first, second], 2);
        Assert.Single(attached);
        Assert.Same(second, attached[0]);
    }

    [Fact]
    public void UnknownVulkanIdentityDoesNotMatchAKnownNativeGpu()
    {
        Assert.Empty(DisplayEnumeration.ForAdapter([Display(1)], null));
    }

    [Fact]
    public void SdlDisplaysArePresentationTargetsOnEachAdapter()
    {
        var target = Display(null);
        Assert.Same(target, Assert.Single(DisplayEnumeration.ForAdapter([target], 1)));
        Assert.Same(target, Assert.Single(DisplayEnumeration.ForAdapter([target], 2)));
        Assert.Same(target, Assert.Single(DisplayEnumeration.ForAdapter([target], null)));
    }

    [Fact]
    public void UninitializedVideoIsDifferentFromNoDisplays()
    {
        Assert.Null(DisplayEnumeration.ForAdapter(null, 1));
        Assert.Empty(DisplayEnumeration.ForAdapter([], 1));
    }

    private static DisplayInfo Display(long? luid) => new("Display",
        new Rectangle(-1920, 0, 1920, 1080), Mode, [Mode], luid);
}
