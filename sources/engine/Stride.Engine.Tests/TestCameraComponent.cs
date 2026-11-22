// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// Tests for the projection of <see cref="CameraComponent"/>.
    /// </summary>
    public class TestCameraComponent
    {
        private static CameraComponent Camera(float nearClipPlane, float farClipPlane, float verticalFieldOfView)
        {
            var camera = new CameraComponent(nearClipPlane, farClipPlane)
            {
                UseCustomViewMatrix = true,
                UseCustomAspectRatio = true,
                AspectRatio = 1.5f,
                VerticalFieldOfView = verticalFieldOfView,
            };
            camera.Update();
            return camera;
        }

        [Fact]
        public void APerspectiveCameraHasTheProjectionOfSystemNumerics()
        {
            var camera = Camera(0.1f, 1000.0f, 45.0f);

            Assert.Equal(Matrix4x4.CreatePerspectiveFieldOfView(MathUtil.DegreesToRadians(45.0f), 1.5f, 0.1f, 1000.0f), camera.ProjectionMatrix);
        }

        [Theory]
        [InlineData(0.0f, 1000.0f, 45.0f)]
        [InlineData(-1.0f, 1000.0f, 45.0f)]
        [InlineData(0.0f, 0.0f, 45.0f)]
        [InlineData(10.0f, 10.0f, 45.0f)]
        [InlineData(10.0f, 5.0f, 45.0f)]
        [InlineData(0.1f, 1000.0f, 0.0f)]
        [InlineData(0.1f, 1000.0f, -10.0f)]
        [InlineData(0.1f, 1000.0f, 180.0f)]
        [InlineData(0.1f, 1000.0f, 400.0f)]
        public void APerspectiveCameraUpdatesWithPlanesOrAnAngleThatHaveNoProjection(float nearClipPlane, float farClipPlane, float verticalFieldOfView)
        {
            var projection = Camera(nearClipPlane, farClipPlane, verticalFieldOfView).ProjectionMatrix;

            Assert.True(float.IsFinite(projection.M11) && float.IsFinite(projection.M22) && float.IsFinite(projection.M33) && float.IsFinite(projection.M43));
            Assert.Equal(-1.0f, projection.M34);
        }
    }
}
