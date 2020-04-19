// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Particles.Tests
{
    public class VisualTestMaterials : GameTest
    {
        public VisualTestMaterials() : base("VisualTestMaterials") { }

        [Fact]
        public void RunVisualTests()
        {
            RunGameTest(new VisualTestMaterials());
        }
    }
}
