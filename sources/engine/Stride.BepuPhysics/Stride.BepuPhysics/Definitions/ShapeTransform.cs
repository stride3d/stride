using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions;

public struct ShapeTransform
{
    public Vector3 PositionLocal = Vector3.Zero;
    public Quaternion RotationLocal = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;

    public ShapeTransform()
    {
    }
}