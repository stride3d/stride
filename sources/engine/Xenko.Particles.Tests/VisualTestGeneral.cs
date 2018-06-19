// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;

namespace Xenko.Particles.Tests
{
    class VisualTestGeneral : GameTest
    {
        public VisualTestGeneral() : base("VisualTestGeneral")
        {
            IndividualTestVersion = 1;  //  Changes in particle spawning
        }

        [Test]
        public void RunVisualTests()
        {
            RunGameTest(new VisualTestGeneral());
        }
    }
}
