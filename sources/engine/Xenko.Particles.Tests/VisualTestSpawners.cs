// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Xenko.Particles.Tests
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
