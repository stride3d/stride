// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Xunit;

namespace Stride.Assets.Editor.Tests;

public sealed class EditorGameDiscoveryTests
{
    // Types no editor game discovers, so that excluding them does not change the other tests
    private sealed class FirstType;
    private sealed class SecondType;

    [Fact]
    public void AnExcludedTypeComesBackWhenItsExclusionIsDisposed()
    {
        // The cross-platform editor leaves nothing out
        Assert.False(EditorGameDiscovery.IsExcluded(typeof(Stride.Assets.Editor.EditorGame.Tools.EditorGameToolService)));

        using (EditorGameDiscovery.Exclude(typeof(FirstType), typeof(SecondType)))
        {
            Assert.True(EditorGameDiscovery.IsExcluded(typeof(FirstType)));
            Assert.True(EditorGameDiscovery.IsExcluded(typeof(SecondType)));

            // Excluding a type twice: the second exclusion does not put it back
            EditorGameDiscovery.Exclude(typeof(FirstType)).Dispose();
            Assert.True(EditorGameDiscovery.IsExcluded(typeof(FirstType)));
        }

        Assert.False(EditorGameDiscovery.IsExcluded(typeof(FirstType)));
        Assert.False(EditorGameDiscovery.IsExcluded(typeof(SecondType)));
    }
}
