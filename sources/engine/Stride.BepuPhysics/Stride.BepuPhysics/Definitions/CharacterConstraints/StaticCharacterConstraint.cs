using Stride.BepuPhysics.Components.Constraints;

namespace Stride.BepuPhysics.Definitions.CharacterConstraints;
public sealed class StaticCharacterConstraint : ConstraintComponent<StaticCharacterMotionConstraint>
{
    public StaticCharacterConstraint() => BepuConstraint = new();
}