// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;
using Xenko.Graphics;

namespace Xenko.Particles.Tests
{
    class VisualTestSoftEdge : GameTest
    {
        public VisualTestSoftEdge() : base("VisualTestSoftEdge") { }

        [Test]
        public void RunVisualTests10()
        {
            RunGameTest(new GameTest("VisualTestSoftEdge", GraphicsProfile.Level_10_0));
        }

        [Test]
        public void RunVisualTests11()
        {
            RunGameTest(new GameTest("VisualTestSoftEdge", GraphicsProfile.Level_11_0));
        }

    }
}
