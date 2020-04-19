// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;
using Stride.Core.Mathematics;

namespace Stride.Input.Tests
{
    public class TestDirection
    {
        [Fact]
        public void RunDirectionTests()
        {
            Vector2 inputA = new Vector2(1.0f, 0.0f);
            Direction a = new Direction(inputA);

            // Check not neutral
            Assert.False(a.IsNeutral);

            // Check if input matches converted output vector
            Assert.Equal(inputA, (Vector2)a);

            Assert.NotEqual(Direction.None, a);
            Assert.Equal(Direction.Right, a);
            Assert.NotEqual(Direction.RightUp, a);

            Vector2 inputB = new Vector2(0.0f, 0.0f);
            Direction b = new Direction(inputB);

            // Check neutral
            Assert.True(b.IsNeutral);

            // Check if input matches converted output vector
            Assert.Equal(inputB, (Vector2)b);

            Assert.Equal(Direction.None, b);
            Assert.NotEqual(b, a);

            Direction c = Direction.FromTicks(2000, 4000);
            Assert.Equal(Direction.Down, c);

            // Check conversion to ticks
            Assert.Equal(2000, a.GetTicks(8000));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.GetTicks(0));
            Assert.Throws<InvalidOperationException>(() => b.GetTicks(4000));
            Assert.Equal(2, c.GetTicks(4));
            Assert.Equal(4, c.GetTicks(8));
            Assert.Equal(2000, c.GetTicks(4000));
            Assert.Equal(4000, c.GetTicks(8000));
        }

        [Fact]
        public void TestDPadConversion()
        {
            Assert.Equal(Direction.Right, GameControllerUtils.ButtonsToDirection(GamePadButton.PadRight));
            Assert.Equal(Direction.Left, GameControllerUtils.ButtonsToDirection(GamePadButton.PadLeft));
            Assert.Equal(Direction.None, GameControllerUtils.ButtonsToDirection(GamePadButton.None));
            Assert.Equal(GamePadButton.PadRight, GameControllerUtils.DirectionToButtons(Direction.Right));
            Assert.Equal(GamePadButton.PadLeft, GameControllerUtils.DirectionToButtons(Direction.Left));
            Assert.Equal(GamePadButton.None, GameControllerUtils.DirectionToButtons(Direction.None));

            // Test rounding
            Assert.Equal(GamePadButton.PadUp, GameControllerUtils.DirectionToButtons(Direction.FromTicks(99,100)));
            Assert.Equal(GamePadButton.PadUp, GameControllerUtils.DirectionToButtons(Direction.FromTicks(1, 100)));
            Assert.Equal(GamePadButton.PadDown, GameControllerUtils.DirectionToButtons(Direction.FromTicks(49, 100)));
            Assert.Equal(GamePadButton.PadDown, GameControllerUtils.DirectionToButtons(Direction.FromTicks(51, 100)));
        }
    }
}
