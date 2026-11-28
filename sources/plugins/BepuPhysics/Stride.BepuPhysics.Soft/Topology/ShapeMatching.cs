// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Soft.Topology;

/// <summary>
/// Finds the rotation that best maps the rest shape of a set of points onto their current positions.
/// </summary>
internal static class ShapeMatching
{
    /// <summary>
    /// Refines <paramref name="rotation"/> towards the rotational part of the covariance matrix given by its columns,
    /// see 'A Robust Method to Extract the Rotational Part of Deformations', Müller et al. 2016.
    /// </summary>
    public static Quaternion ExtractRotation(Vector3 column0, Vector3 column1, Vector3 column2, Quaternion rotation, int iterations = 8)
    {
        for (int i = 0; i < iterations; i++)
        {
            var x = Vector3.Transform(Vector3.UnitX, rotation);
            var y = Vector3.Transform(Vector3.UnitY, rotation);
            var z = Vector3.Transform(Vector3.UnitZ, rotation);
            var omega = Vector3.Cross(x, column0) + Vector3.Cross(y, column1) + Vector3.Cross(z, column2);
            omega /= MathF.Abs(Vector3.Dot(x, column0) + Vector3.Dot(y, column1) + Vector3.Dot(z, column2)) + 1e-9f;

            var angle = omega.Length();
            if (angle < 1e-9f)
                break;

            rotation = Quaternion.Normalize(rotation * Quaternion.RotationAxis(omega / angle, angle));
        }

        return rotation;
    }
}
