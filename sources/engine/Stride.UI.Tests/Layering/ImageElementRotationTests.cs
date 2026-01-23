// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.UI.Controls;
using Xunit;

namespace Stride.UI.Tests.Layering
{
    /// <summary>
    /// Tests for the <see cref="ImageElement.Rotation"/> property.
    /// </summary>
    [System.ComponentModel.Description("Tests for ImageElement rotation functionality")]
    public class ImageElementRotationTests
    {
        [Fact]
        [System.ComponentModel.Description("Test that the default rotation value is 0")]
        public void TestDefaultRotation()
        {
            var image = new ImageElement();
            Assert.Equal(0f, image.Rotation);
        }

        [Fact]
        [System.ComponentModel.Description("Test that the default LocalMatrix is Identity when rotation is 0")]
        public void TestDefaultLocalMatrix()
        {
            var image = new ImageElement();
            Assert.Equal(Matrix.Identity, image.LocalMatrix);
        }

        [Fact]
        [System.ComponentModel.Description("Test setting rotation to a positive value")]
        public void TestSetPositiveRotation()
        {
            var image = new ImageElement();
            var angle = MathUtil.PiOverFour; // 45 degrees

            image.Rotation = angle;

            Assert.Equal(angle, image.Rotation);
        }

        [Fact]
        [System.ComponentModel.Description("Test setting rotation to a negative value (counter-clockwise)")]
        public void TestSetNegativeRotation()
        {
            var image = new ImageElement();
            var angle = -MathUtil.PiOverFour; // -45 degrees

            image.Rotation = angle;

            Assert.Equal(angle, image.Rotation);
        }

        [Fact]
        [System.ComponentModel.Description("Test that LocalMatrix is updated when rotation changes")]
        public void TestLocalMatrixUpdatesOnRotationChange()
        {
            var image = new ImageElement();
            var angle = MathUtil.PiOverTwo; // 90 degrees

            image.Rotation = angle;

            var expectedMatrix = Matrix.RotationZ(angle);
            AssertMatrixEqual(expectedMatrix, image.LocalMatrix);
        }

        [Fact]
        [System.ComponentModel.Description("Test that setting rotation to 0 resets LocalMatrix to Identity")]
        public void TestRotationZeroResetsToIdentity()
        {
            var image = new ImageElement();

            // First set a non-zero rotation
            image.Rotation = MathUtil.PiOverFour;
            Assert.NotEqual(Matrix.Identity, image.LocalMatrix);

            // Then reset to zero
            image.Rotation = 0f;
            Assert.Equal(Matrix.Identity, image.LocalMatrix);
        }

        [Fact]
        [System.ComponentModel.Description("Test that very small rotation values (near zero) set LocalMatrix to Identity")]
        public void TestVerySmallRotationSetsIdentity()
        {
            var image = new ImageElement();

            // Set a value smaller than float.Epsilon
            image.Rotation = float.Epsilon / 2f;

            // Should be treated as zero
            Assert.Equal(Matrix.Identity, image.LocalMatrix);
        }

        [Fact]
        [System.ComponentModel.Description("Test multiple rotation changes")]
        public void TestMultipleRotationChanges()
        {
            var image = new ImageElement();

            // First rotation
            image.Rotation = MathUtil.PiOverFour;
            AssertMatrixEqual(Matrix.RotationZ(MathUtil.PiOverFour), image.LocalMatrix);

            // Second rotation
            image.Rotation = MathUtil.PiOverTwo;
            AssertMatrixEqual(Matrix.RotationZ(MathUtil.PiOverTwo), image.LocalMatrix);

            // Third rotation (negative)
            image.Rotation = -MathUtil.PiOverFour;
            AssertMatrixEqual(Matrix.RotationZ(-MathUtil.PiOverFour), image.LocalMatrix);
        }

        [Fact]
        [System.ComponentModel.Description("Test that setting the same rotation value doesn't trigger unnecessary updates")]
        public void TestSetSameRotationValue()
        {
            var image = new ImageElement();
            var angle = MathUtil.PiOverFour;

            image.Rotation = angle;
            var firstMatrix = image.LocalMatrix;

            // Set the same value again
            image.Rotation = angle;
            var secondMatrix = image.LocalMatrix;

            // Matrix should be the same
            Assert.Equal(firstMatrix, secondMatrix);
        }

        [Fact]
        [System.ComponentModel.Description("Test that rotation doesn't affect the image size or measurement")]
        public void TestRotationDoesNotAffectMeasurement()
        {
            var sprite = new Sprite()
            {
                Region = new Rectangle(0, 0, 100, 50)
            };
            var image = new ImageElement()
            {
                Source = (Rendering.Sprites.SpriteFromTexture)sprite,
                StretchType = StretchType.None
            };

            // Measure without rotation
            image.Measure(new Vector3(200, 200, 0));
            var sizeWithoutRotation = image.DesiredSizeWithMargins;

            // Apply rotation and measure again
            image.Rotation = MathUtil.PiOverFour;
            image.Measure(new Vector3(200, 200, 0));
            var sizeWithRotation = image.DesiredSizeWithMargins;

            // Rotation should not change the measured size
            Assert.Equal(sizeWithoutRotation, sizeWithRotation);
        }

        /// <summary>
        /// Helper method to assert that two matrices are approximately equal within a tolerance.
        /// </summary>
        private static void AssertMatrixEqual(Matrix expected, Matrix actual, int precision = 5)
        {
            Assert.Equal(expected.M11, actual.M11, precision);
            Assert.Equal(expected.M12, actual.M12, precision);
            Assert.Equal(expected.M13, actual.M13, precision);
            Assert.Equal(expected.M14, actual.M14, precision);

            Assert.Equal(expected.M21, actual.M21, precision);
            Assert.Equal(expected.M22, actual.M22, precision);
            Assert.Equal(expected.M23, actual.M23, precision);
            Assert.Equal(expected.M24, actual.M24, precision);

            Assert.Equal(expected.M31, actual.M31, precision);
            Assert.Equal(expected.M32, actual.M32, precision);
            Assert.Equal(expected.M33, actual.M33, precision);
            Assert.Equal(expected.M34, actual.M34, precision);

            Assert.Equal(expected.M41, actual.M41, precision);
            Assert.Equal(expected.M42, actual.M42, precision);
            Assert.Equal(expected.M43, actual.M43, precision);
            Assert.Equal(expected.M44, actual.M44, precision);
        }
    }
}
