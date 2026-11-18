// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using Xunit;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml.Tests.Serialization;

public class NumericsTests
{
    public class Values
    {
        public Vector2 Size;
        public Vector3 Position;
        public Vector4 Color;
        public Quaternion Rotation;
        public Plane Ground;
        public Matrix4x4 Transform;
    }

    [Fact]
    public void SystemNumericsTypesAreWrittenAsTheMathTypesOfStride()
    {
        var serializer = new Serializer();
        var values = new Values
        {
            Size = new(1, 2),
            Position = new(1, 2, 3),
            Color = new(1, 2, 3, 4),
            Rotation = new(0, 0, 0, 1),
            Ground = new(0, 1, 0, 5),
            Transform = Matrix4x4.CreateTranslation(7, 8, 9),
        };

        var text = serializer.Serialize(values, typeof(Values));

        Assert.Equal("""
            Size: {X: 1.0, Y: 2.0}
            Position: {X: 1.0, Y: 2.0, Z: 3.0}
            Color: {X: 1.0, Y: 2.0, Z: 3.0, W: 4.0}
            Rotation: {X: 0.0, Y: 0.0, Z: 0.0, W: 1.0}
            Ground:
              Normal: {X: 0.0, Y: 1.0, Z: 0.0}
              D: 5.0
            Transform: {M11: 1.0, M12: 0.0, M13: 0.0, M14: 0.0, M21: 0.0, M22: 1.0, M23: 0.0, M24: 0.0, M31: 0.0, M32: 0.0, M33: 1.0, M34: 0.0, M41: 7.0, M42: 8.0, M43: 9.0, M44: 1.0}

            """.ReplaceLineEndings("\n"), text.ReplaceLineEndings("\n"));

        var read = (Values)serializer.Deserialize(text, typeof(Values))!;
        Assert.Equal(values.Size, read.Size);
        Assert.Equal(values.Position, read.Position);
        Assert.Equal(values.Color, read.Color);
        Assert.Equal(values.Rotation, read.Rotation);
        Assert.Equal(values.Ground, read.Ground);
        Assert.Equal(values.Transform, read.Transform);
    }
}
