// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core;
using Stride.Games;

namespace Stride.Engine.Tests;

/// <summary>
/// Tests the content loading of <see cref="GameSystemCollection"/>: only systems whose content loaded are unloaded.
/// </summary>
public class GameSystemCollectionTest
{
    private class TestSystem : ComponentBase, IGameSystemBase, IContentable
    {
        public bool FailLoad;
        public int LoadCount;
        public int UnloadCount;

        public void Initialize()
        {
        }

        public void LoadContent()
        {
            LoadCount++;
            if (FailLoad)
                throw new InvalidOperationException("Load failed");
        }

        public void UnloadContent() => UnloadCount++;
    }

    [Fact]
    public void UnloadContentUnloadsEachLoadedSystemOnce()
    {
        var systems = new GameSystemCollection(new ServiceRegistry());
        var first = new TestSystem();
        var second = new TestSystem();
        systems.Add(first);
        systems.Add(second);
        systems.Initialize();

        systems.LoadContent();
        systems.UnloadContent();

        Assert.Equal(1, first.UnloadCount);
        Assert.Equal(1, second.UnloadCount);
    }

    [Fact]
    public void UnloadContentSkipsSystemsWhoseContentDidNotLoad()
    {
        var systems = new GameSystemCollection(new ServiceRegistry());
        var loaded = new TestSystem();
        var failing = new TestSystem { FailLoad = true };
        var notReached = new TestSystem();
        systems.Add(loaded);
        systems.Add(failing);
        systems.Add(notReached);
        systems.Initialize();

        Assert.Throws<InvalidOperationException>(systems.LoadContent);
        // What the game does when its graphics device is disposed
        systems.UnloadContent();

        Assert.Equal(1, loaded.UnloadCount);
        Assert.Equal(0, failing.UnloadCount);
        Assert.Equal(0, notReached.LoadCount);
        Assert.Equal(0, notReached.UnloadCount);
    }
}
