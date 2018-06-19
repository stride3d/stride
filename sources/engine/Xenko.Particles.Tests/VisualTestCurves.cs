// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;

namespace Xenko.Particles.Tests
{
    class VisualTestCurves : GameTest
    {
        public VisualTestCurves() : base("VisualTestCurves")
        {
            IndividualTestVersion = 1;  //  Changed the default rotation for paritcles using curve rotation
        }

        [Test]
        public void RunVisualTests()
        {
            RunGameTest(new VisualTestCurves());
        }
    }
}
