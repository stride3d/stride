using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Definitions.CharacterConstraints;

//Each constraint type has its own 'type processor'- it acts as the outer loop that handles all the common logic across batches of constraints and invokes
//the per-constraint logic as needed. The CharacterMotionFunctions type provides the actual implementation.
public class StaticCharacterMotionTypeProcessor : OneBodyTypeProcessor<StaticCharacterMotionPrestep, CharacterMotionAccumulatedImpulse, StaticCharacterMotionFunctions, AccessAll, AccessAll>
{
    /// <summary>
    /// Simulation-wide unique id for the character motion constraint. Every type has needs a unique compile time id; this is a little bit annoying to guarantee given that there is no central
    /// registry of all types that can exist (custom ones, like this one, can always be created), but having it be constant helps simplify and optimize its internal usage.
    /// </summary>
    public const int BatchTypeId = 50;
}
