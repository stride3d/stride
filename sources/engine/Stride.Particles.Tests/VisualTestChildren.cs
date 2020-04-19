// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Particles.Tests
{
    public class VisualTestChildren : GameTest
    {
        public VisualTestChildren() : base("VisualTestChildren")
        {
            IndividualTestVersion = 1;  //  Changes in particle spawning
            IndividualTestVersion += 4;  //  Changed to avoid collisions with 1.10
        }

        [Fact]
        public void RunVisualTests()
        {
            RunGameTest(new VisualTestChildren());
        }
    }
}
