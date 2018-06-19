// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using NUnit.Framework;
using Xenko.Core.Mathematics;

namespace Xenko.Input.Tests
{
    [TestFixture]
    public class TestDirection
    {
        [Test]
        public void RunDirectionTests()
        {
            Vector2 inputA = new Vector2(1.0f, 0.0f);
            Direction a = new Direction(inputA);

            // Check not neutral
            Assert.IsFalse(a.IsNeutral);

            // Check if input matches converted output vector
            Assert.AreEqual(inputA, (Vector2)a);

            Assert.AreNotEqual(Direction.None, a);
            Assert.AreEqual(Direction.Right, a);
            Assert.AreNotEqual(Direction.RightUp, a);

            Vector2 inputB = new Vector2(0.0f, 0.0f);
            Direction b = new Direction(inputB);

            // Check neutral
            Assert.IsTrue(b.IsNeutral);

            // Check if input matches converted output vector
            Assert.AreEqual(inputB, (Vector2)b);

            Assert.AreEqual(Direction.None, b);
            Assert.AreNotEqual(b, a);

            Direction c = Direction.FromTicks(2000, 4000);
            Assert.AreEqual(Direction.Down, c);

            // Check conversion to ticks
            Assert.AreEqual(2000, a.GetTicks(8000));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.GetTicks(0));
            Assert.Throws<InvalidOperationException>(() => b.GetTicks(4000));
            Assert.AreEqual(2, c.GetTicks(4));
            Assert.AreEqual(4, c.GetTicks(8));
            Assert.AreEqual(2000, c.GetTicks(4000));
            Assert.AreEqual(4000, c.GetTicks(8000));
        }

        [Test]
        public void TestDPadConversion()
        {
            Assert.AreEqual(Direction.Right, GameControllerUtils.ButtonsToDirection(GamePadButton.PadRight));
            Assert.AreEqual(Direction.Left, GameControllerUtils.ButtonsToDirection(GamePadButton.PadLeft));
            Assert.AreEqual(Direction.None, GameControllerUtils.ButtonsToDirection(GamePadButton.None));
            Assert.AreEqual(GamePadButton.PadRight, GameControllerUtils.DirectionToButtons(Direction.Right));
            Assert.AreEqual(GamePadButton.PadLeft, GameControllerUtils.DirectionToButtons(Direction.Left));
            Assert.AreEqual(GamePadButton.None, GameControllerUtils.DirectionToButtons(Direction.None));

            // Test rounding
            Assert.AreEqual(GamePadButton.PadUp, GameControllerUtils.DirectionToButtons(Direction.FromTicks(99,100)));
            Assert.AreEqual(GamePadButton.PadUp, GameControllerUtils.DirectionToButtons(Direction.FromTicks(1, 100)));
            Assert.AreEqual(GamePadButton.PadDown, GameControllerUtils.DirectionToButtons(Direction.FromTicks(49, 100)));
            Assert.AreEqual(GamePadButton.PadDown, GameControllerUtils.DirectionToButtons(Direction.FromTicks(51, 100)));
        }
    }
}
