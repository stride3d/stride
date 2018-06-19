// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;

namespace Xenko.Particles.Tests
{
    class VisualTestChildren : GameTest
    {
        public VisualTestChildren() : base("VisualTestChildren")
        {
            IndividualTestVersion = 1;  //  Changes in particle spawning
            IndividualTestVersion += 4;  //  Changed to avoid collisions with 1.10
        }

        [Test]
        public void RunVisualTests()
        {
            RunGameTest(new VisualTestChildren());
        }
    }
}
