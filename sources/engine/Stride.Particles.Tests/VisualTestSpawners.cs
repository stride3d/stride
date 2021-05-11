// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Particles.Tests
{
    public class VisualTestSpawners : GameTest
    {
        public VisualTestSpawners() : base("VisualTestSpawners") { }

        [Fact]
        public void RunVisualTests()
        {
            RunGameTest(new VisualTestSpawners());
        }
    }
}
