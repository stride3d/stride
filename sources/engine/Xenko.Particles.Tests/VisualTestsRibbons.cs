// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;

namespace Xenko.Particles.Tests
{
    class VisualTestRibbons : GameTest
    {
        public VisualTestRibbons() : base("VisualTestRibbons")
        {
            IndividualTestVersion = 2;  // Negligible, but consistent differences in the two images (~4-5 pixels total)
            IndividualTestVersion += 4;  //  Changed to avoid collisions with 1.10
        }

        [Test]
        public void RunVisualTests()
        {
            RunGameTest(new VisualTestRibbons());
        }
    }
}
