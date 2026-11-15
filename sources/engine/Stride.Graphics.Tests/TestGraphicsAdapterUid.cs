// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Xunit;

namespace Stride.Graphics.Tests
{
    public class TestGraphicsAdapterUid : GraphicTestGameBase
    {
        // The last adapter is one the device would not take by default, when there are several
        private readonly GraphicsAdapter requiredAdapter;

        public TestGraphicsAdapterUid()
        {
            requiredAdapter = GraphicsAdapterFactory.Adapters[^1];
            GraphicsDeviceManager.RequiredAdapterUid = requiredAdapter.AdapterUid.ToString();
        }

        /// <summary>
        /// <c>GraphicsDeviceManager.RequiredAdapterUid</c> picks an adapter by <see cref="GraphicsAdapter.AdapterUid"/>,
        /// so every adapter needs its own.
        /// </summary>
        [Fact]
        public void EveryAdapterHasItsOwnUid()
        {
            var uids = new HashSet<long>();
            foreach (var adapter in GraphicsAdapterFactory.Adapters)
            {
                Assert.True(adapter.AdapterUid != 0, $"Adapter {adapter.Description} has no uid");
                Assert.True(uids.Add(adapter.AdapterUid), $"Adapter {adapter.Description} has the same uid as another adapter: {adapter.AdapterUid}");
            }
        }

        [SkippableFact]
        public void RequiredAdapterUidPicksThatAdapter()
        {
            PerformTest(game =>
            {
                var adapter = game.GraphicsDevice.Adapter;
                Assert.True(adapter == requiredAdapter, $"Required {requiredAdapter.Description}, got {adapter.Description}");
            });
        }
    }
}
