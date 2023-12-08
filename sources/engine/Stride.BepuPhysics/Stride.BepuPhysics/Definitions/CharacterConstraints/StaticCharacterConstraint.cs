using Stride.BepuPhysics.Components.Constraints;

namespace Stride.BepuPhysics.Definitions.CharacterConstraints;
public class StaticCharacterConstraint : ConstraintComponent
{
    internal StaticCharacterMotionConstraint _bepuConstraint = new();
}