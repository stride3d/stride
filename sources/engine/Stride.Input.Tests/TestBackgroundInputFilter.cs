// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Input.Tests
{
    /// <summary>
    /// Tests the rules applied to game controllers while the game window has no focus, which follow SDL's.
    /// </summary>
    public class TestBackgroundInputFilter
    {
        [Fact]
        public void ButtonsCanOnlyBeReleased()
        {
            Assert.True(BackgroundInputFilter.AcceptsButton(isDown: false));
            Assert.False(BackgroundInputFilter.AcceptsButton(isDown: true));
        }

        [Fact]
        public void DirectionsCanOnlyReturnToNeutral()
        {
            Assert.True(BackgroundInputFilter.AcceptsDirection(Direction.None));
            Assert.False(BackgroundInputFilter.AcceptsDirection(Direction.Up));
        }

        [Theory]
        [InlineData(0.0f, 0.8f, 0.3f, true)]
        [InlineData(0.0f, -0.8f, -0.3f, true)]
        [InlineData(0.0f, 0.8f, 0.0f, true)]
        [InlineData(0.0f, 0.3f, 0.8f, false)]
        [InlineData(0.0f, -0.3f, -0.8f, false)]
        [InlineData(0.0f, 0.0f, 0.5f, false)]
        [InlineData(0.0f, 0.8f, -0.3f, false)]
        [InlineData(-1.0f, -0.2f, -0.6f, true)]
        [InlineData(-1.0f, -0.6f, -0.2f, false)]
        public void AxesCanOnlyMoveTowardsRest(float rest, float current, float value, bool accepted)
        {
            Assert.Equal(accepted, BackgroundInputFilter.AcceptsAxis(rest, current, value));
        }

        [Fact]
        public void RestIsTheFirstValue()
        {
            var rest = new AxisRest();
            rest.Observe(0.1f);
            rest.Observe(0.6f);
            rest.Observe(-0.4f);

            Assert.Equal(0.1f, rest.Value);
        }

        [Fact]
        public void RestIsTakenAgainWhenTheFirstValueIsAtAnEndAndTheNextIsCentred()
        {
            var rest = new AxisRest();
            rest.Observe(-1.0f);
            rest.Observe(0.05f);

            Assert.Equal(0.05f, rest.Value);
        }

        [Fact]
        public void RestStaysAtAnEndOnceTheAxisHasMovedElsewhere()
        {
            var rest = new AxisRest();
            rest.Observe(-1.0f);
            rest.Observe(-0.5f);
            rest.Observe(0.05f);

            Assert.Equal(-1.0f, rest.Value);
        }
    }
}
