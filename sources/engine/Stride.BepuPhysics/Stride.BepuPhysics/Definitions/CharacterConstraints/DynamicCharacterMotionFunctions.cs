using BepuPhysics.Constraints;
using BepuPhysics;
using BepuUtilities;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Stride.BepuPhysics.Definitions.CharacterConstraints;

public struct DynamicCharacterMotionFunctions : ITwoBodyConstraintFunctions<DynamicCharacterMotionPrestep, CharacterMotionAccumulatedImpulse>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ComputeJacobians(in Vector3Wide offsetA, in Vector3Wide offsetB, in QuaternionWide basisQuaternion,
        out Matrix3x3Wide basis,
        out Matrix2x3Wide horizontalAngularJacobianA, out Matrix2x3Wide horizontalAngularJacobianB,
        out Vector3Wide verticalAngularJacobianA, out Vector3Wide verticalAngularJacobianB)
    {
        //Both of the motion constraints are velocity motors, like tangent friction. They don't actually have a position level goal.
        //But if we did want to make such a position level goal, it could be expressed as:
        //dot(basis.X, constrainedPointOnA - constrainedPointOnB) = 0
        //dot(basis.Y, constrainedPointOnA - constrainedPointOnB) <= 0 
        //dot(basis.Z, constrainedPointOnA - constrainedPointOnB) = 0
        //Note that the Y axis, corresponding to the vertical motion constraint, is an inequality. It pulls toward the surface, but never pushes away.
        //It also has a separate maximum force and acts on an independent axis; that's why we solve it as a separate constraint.
        //To get a velocity constraint out of these position goals, differentiate with respect to time:
        //d/dt(dot(basis.X, constrainedPointOnA - constrainedPointOnB)) = dot(basis.X, d/dt(constrainedPointOnA - constrainedPointOnB))
        //                                                              = dot(basis.X, a.LinearVelocity + a.AngularVelocity x offsetToConstrainedPointOnA - b.linearVelocity - b.AngularVelocity x offsetToConstrainedPointOnB)
        //Throwing some algebra and identities at it:
        //dot(basis.X, a.LinearVelocity) + dot(basis.X, a.AngularVelocity x offsetToConstrainedPointOnA) + dot(-basis.X, b.LinearVelocity) + dot(basis.X, offsetToConstrainedPointOnB x b.AngularVelocity)
        //dot(basis.X, a.LinearVelocity) + dot(a.AngularVelocity, offsetToConstrainedPointOnA x basis.X) + dot(-basis.X, b.LinearVelocity) + dot(b.AngularVelocity, basis.X x offsetToConstrainedPointOnB)
        //The (transpose) jacobian is the transform that pulls the body velocity into constraint space- 
        //and here, we can see that we have an axis being dotted with each component of the velocity. That's gives us the jacobian for that degree of freedom.
        //The same form applies to all three axes of the basis, since they're all doing the same thing (just on different directions and with different force bounds).
        //Note that we don't explicitly output linear jacobians- they are just the axes of the basis, and the linear jacobians of B are just the negated linear jacobians of A.
        Matrix3x3Wide.CreateFromQuaternion(basisQuaternion, out basis);
        Vector3Wide.CrossWithoutOverlap(offsetA, basis.X, out horizontalAngularJacobianA.X);
        Vector3Wide.CrossWithoutOverlap(offsetA, basis.Y, out verticalAngularJacobianA);
        Vector3Wide.CrossWithoutOverlap(offsetA, basis.Z, out horizontalAngularJacobianA.Y);
        Vector3Wide.CrossWithoutOverlap(basis.X, offsetB, out horizontalAngularJacobianB.X);
        Vector3Wide.CrossWithoutOverlap(basis.Y, offsetB, out verticalAngularJacobianB);
        Vector3Wide.CrossWithoutOverlap(basis.Z, offsetB, out horizontalAngularJacobianB.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyHorizontalImpulse(in Matrix3x3Wide basis,
        in Matrix2x3Wide angularJacobianA, in Matrix2x3Wide angularJacobianB, in Vector2Wide constraintSpaceImpulse,
        in BodyInertiaWide inertiaA, in BodyInertiaWide inertiaB,
        ref BodyVelocityWide velocityA, ref BodyVelocityWide velocityB)
    {
        //Transform the constraint space impulse into world space by using the jacobian and then apply each body's inverse inertia to get the velocity change.
        Vector3Wide.Scale(basis.X, constraintSpaceImpulse.X, out var linearImpulseAX);
        Vector3Wide.Scale(basis.Z, constraintSpaceImpulse.Y, out var linearImpulseAY);
        Vector3Wide.Add(linearImpulseAX, linearImpulseAY, out var linearImpulseA);
        Vector3Wide.Scale(linearImpulseA, inertiaA.InverseMass, out var linearChangeA);
        Vector3Wide.Add(velocityA.Linear, linearChangeA, out velocityA.Linear);
        Vector3Wide.Scale(linearImpulseA, inertiaB.InverseMass, out var negatedLinearChangeB); //Linear jacobians for B are just A's negated linear jacobians.
        Vector3Wide.Subtract(velocityB.Linear, negatedLinearChangeB, out velocityB.Linear);

        Matrix2x3Wide.Transform(constraintSpaceImpulse, angularJacobianA, out var angularImpulseA);
        Symmetric3x3Wide.TransformWithoutOverlap(angularImpulseA, inertiaA.InverseInertiaTensor, out var angularChangeA);
        Vector3Wide.Add(velocityA.Angular, angularChangeA, out velocityA.Angular);
        Matrix2x3Wide.Transform(constraintSpaceImpulse, angularJacobianB, out var angularImpulseB);
        Symmetric3x3Wide.TransformWithoutOverlap(angularImpulseB, inertiaB.InverseInertiaTensor, out var angularChangeB);
        Vector3Wide.Add(velocityB.Angular, angularChangeB, out velocityB.Angular);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyVerticalImpulse(in Matrix3x3Wide basis,
        in Vector3Wide angularJacobianA, in Vector3Wide angularJacobianB, in Vector<float> constraintSpaceImpulse,
        in BodyInertiaWide inertiaA, in BodyInertiaWide inertiaB,
        ref BodyVelocityWide velocityA, ref BodyVelocityWide velocityB)
    {
        Vector3Wide.Scale(basis.Y, constraintSpaceImpulse, out var linearImpulseA);
        Vector3Wide.Scale(linearImpulseA, inertiaA.InverseMass, out var linearChangeA);
        Vector3Wide.Add(velocityA.Linear, linearChangeA, out velocityA.Linear);
        Vector3Wide.Scale(linearImpulseA, inertiaB.InverseMass, out var negatedLinearChangeB); //Linear jacobians for B are just A's negated linear jacobians.
        Vector3Wide.Subtract(velocityB.Linear, negatedLinearChangeB, out velocityB.Linear);

        Vector3Wide.Scale(angularJacobianA, constraintSpaceImpulse, out var angularImpulseA);
        Symmetric3x3Wide.TransformWithoutOverlap(angularImpulseA, inertiaA.InverseInertiaTensor, out var angularChangeA);
        Vector3Wide.Add(velocityA.Angular, angularChangeA, out velocityA.Angular);
        Vector3Wide.Scale(angularJacobianB, constraintSpaceImpulse, out var angularImpulseB);
        Symmetric3x3Wide.TransformWithoutOverlap(angularImpulseB, inertiaB.InverseInertiaTensor, out var angularChangeB);
        Vector3Wide.Add(velocityB.Angular, angularChangeB, out velocityB.Angular);
    }


    public static void WarmStart(in Vector3Wide positionA, in QuaternionWide orientationA, in BodyInertiaWide inertiaA, in Vector3Wide positionB, in QuaternionWide orientationB, in BodyInertiaWide inertiaB, ref DynamicCharacterMotionPrestep prestep, ref CharacterMotionAccumulatedImpulse accumulatedImpulses, ref BodyVelocityWide velocityA, ref BodyVelocityWide velocityB)
    {
        ComputeJacobians(prestep.OffsetFromCharacter, prestep.OffsetFromSupport, prestep.SurfaceBasis,
            out var basis, out var horizontalAngularJacobianA, out var horizontalAngularJacobianB, out var verticalAngularJacobianA, out var verticalAngularJacobianB);
        ApplyHorizontalImpulse(basis, horizontalAngularJacobianA, horizontalAngularJacobianB, accumulatedImpulses.Horizontal, inertiaA, inertiaB, ref velocityA, ref velocityB);
        ApplyVerticalImpulse(basis, verticalAngularJacobianA, verticalAngularJacobianB, accumulatedImpulses.Vertical, inertiaA, inertiaB, ref velocityA, ref velocityB);
    }

    public static void Solve(in Vector3Wide positionA, in QuaternionWide orientationA, in BodyInertiaWide inertiaA, in Vector3Wide positionB, in QuaternionWide orientationB, in BodyInertiaWide inertiaB, float dt, float inverseDt, ref DynamicCharacterMotionPrestep prestep, ref CharacterMotionAccumulatedImpulse accumulatedImpulses, ref BodyVelocityWide velocityA, ref BodyVelocityWide velocityB)
    {
        //The motion constraint is split into two parts: the horizontal constraint, and the vertical constraint.
        //The horizontal constraint acts almost exactly like the TangentFriction, but we'll duplicate some of the logic to keep this implementation self-contained.
        ComputeJacobians(prestep.OffsetFromCharacter, prestep.OffsetFromSupport, prestep.SurfaceBasis,
            out var basis, out var horizontalAngularJacobianA, out var horizontalAngularJacobianB, out var verticalAngularJacobianA, out var verticalAngularJacobianB);

        //Compute the velocity error by projecting the body velocity into constraint space using the transposed jacobian.
        Vector2Wide horizontalLinearA;
        Vector3Wide.Dot(basis.X, velocityA.Linear, out horizontalLinearA.X);
        Vector3Wide.Dot(basis.Z, velocityA.Linear, out horizontalLinearA.Y);
        Matrix2x3Wide.TransformByTransposeWithoutOverlap(velocityA.Angular, horizontalAngularJacobianA, out var horizontalAngularA);
        Vector2Wide negatedHorizontalLinearB;
        Vector3Wide.Dot(basis.X, velocityB.Linear, out negatedHorizontalLinearB.X);
        Vector3Wide.Dot(basis.Z, velocityB.Linear, out negatedHorizontalLinearB.Y);
        Matrix2x3Wide.TransformByTransposeWithoutOverlap(velocityB.Angular, horizontalAngularJacobianB, out var horizontalAngularB);
        Vector2Wide.Add(horizontalAngularA, horizontalAngularB, out var horizontalAngular);
        Vector2Wide.Subtract(horizontalLinearA, negatedHorizontalLinearB, out var horizontalLinear);
        Vector2Wide.Add(horizontalAngular, horizontalLinear, out var horizontalVelocity);

        //I'll omit the details of where this comes from, but you can check out the other constraints or the sorta-tutorial Inequality1DOF constraint to explain the details,
        //plus some other references. The idea is that we need a way to transform the constraint space velocity (that we get from transforming body velocities
        //by the transpose jacobian) into a corrective impulse for the solver iterations. That corrective impulse is then used to update the velocities on each iteration execution.
        //This transform is the 'effective mass', representing the mass felt by the constraint in its local space.
        //In concept, this constraint is actually two separate constraints solved iteratively, so we have two separate such effective mass transforms.
        Symmetric3x3Wide.MatrixSandwich(horizontalAngularJacobianA, inertiaA.InverseInertiaTensor, out var horizontalAngularContributionA);
        Symmetric3x3Wide.MatrixSandwich(horizontalAngularJacobianB, inertiaB.InverseInertiaTensor, out var horizontalAngularContributionB);
        Symmetric2x2Wide.Add(horizontalAngularContributionA, horizontalAngularContributionB, out var inverseHorizontalEffectiveMass);
        //The linear jacobians are unit length vectors, so J * M^-1 * JT is just M^-1.
        var linearContribution = inertiaA.InverseMass + inertiaB.InverseMass;
        inverseHorizontalEffectiveMass.XX += linearContribution;
        inverseHorizontalEffectiveMass.YY += linearContribution;
        Symmetric2x2Wide.InvertWithoutOverlap(inverseHorizontalEffectiveMass, out var horizontalEffectiveMass);

        Vector2Wide horizontalConstraintSpaceVelocityChange;
        horizontalConstraintSpaceVelocityChange.X = prestep.TargetVelocity.X - horizontalVelocity.X;
        //The surface basis's Z axis points in the opposite direction to the view direction, so negate the target velocity along the Z axis to point it in the expected direction.
        horizontalConstraintSpaceVelocityChange.Y = -prestep.TargetVelocity.Y - horizontalVelocity.Y;
        Symmetric2x2Wide.TransformWithoutOverlap(horizontalConstraintSpaceVelocityChange, horizontalEffectiveMass, out var horizontalCorrectiveImpulse);

        //Limit the force applied by the horizontal motion constraint. Note that this clamps the *accumulated* impulse applied this time step, not just this one iterations' value.
        var previousHorizontalAccumulatedImpulse = accumulatedImpulses.Horizontal;
        Vector2Wide.Add(accumulatedImpulses.Horizontal, horizontalCorrectiveImpulse, out accumulatedImpulses.Horizontal);
        Vector2Wide.Length(accumulatedImpulses.Horizontal, out var horizontalImpulseMagnitude);
        //Note division by zero guard.
        var dtWide = new Vector<float>(dt);
        var maximumHorizontalImpulse = prestep.MaximumHorizontalForce * dtWide;
        var scale = Vector.Min(Vector<float>.One, maximumHorizontalImpulse / Vector.Max(new Vector<float>(1e-16f), horizontalImpulseMagnitude));
        Vector2Wide.Scale(accumulatedImpulses.Horizontal, scale, out accumulatedImpulses.Horizontal);
        Vector2Wide.Subtract(accumulatedImpulses.Horizontal, previousHorizontalAccumulatedImpulse, out horizontalCorrectiveImpulse);

        ApplyHorizontalImpulse(basis, horizontalAngularJacobianA, horizontalAngularJacobianB, horizontalCorrectiveImpulse, inertiaA, inertiaB, ref velocityA, ref velocityB);

        //Same thing for the vertical constraint.
        Vector3Wide.Dot(basis.Y, velocityA.Linear, out var verticalLinearA);
        Vector3Wide.Dot(velocityA.Angular, verticalAngularJacobianA, out var verticalAngularA);
        Vector3Wide.Dot(basis.Y, velocityB.Linear, out var negatedVerticalLinearB);
        Vector3Wide.Dot(velocityB.Angular, verticalAngularJacobianB, out var verticalAngularB);
        //If the character is deeply penetrating, the vertical motion constraint will allow some separating velocity- just enough for one frame of integration to reach zero depth.
        var verticalBiasVelocity = Vector.Max(Vector<float>.Zero, prestep.Depth * inverseDt);

        //The vertical constraint just targets zero velocity, but does not attempt to fight any velocity which would merely push the character out of penetration.
        //Note that many characters will just have zero inverse inertia tensors to prevent them from rotating, so this could be optimized.
        //We don't take advantage of this optimization for simplicity, and so that you could use this constraint unchanged in a simulation
        //where the orientation is instead controlled by some other constraint or torque- imagine a game with gravity that points in different directions.
        Symmetric3x3Wide.VectorSandwich(verticalAngularJacobianA, inertiaA.InverseInertiaTensor, out var verticalAngularContributionA);
        Symmetric3x3Wide.VectorSandwich(verticalAngularJacobianB, inertiaB.InverseInertiaTensor, out var verticalAngularContributionB);
        var inverseVerticalEffectiveMass = verticalAngularContributionA + verticalAngularContributionB + linearContribution;
        var verticalCorrectiveImpulse = (verticalBiasVelocity - verticalLinearA + negatedVerticalLinearB - verticalAngularA - verticalAngularB) / inverseVerticalEffectiveMass;

        //Clamp the vertical constraint's impulse, but note that this is a bit different than above- the vertical constraint is not allowed to *push*, so there's an extra bound at zero.
        var previousVerticalAccumulatedImpulse = accumulatedImpulses.Vertical;
        var maximumVerticalImpulse = prestep.MaximumVerticalForce * dtWide;
        accumulatedImpulses.Vertical = Vector.Min(Vector<float>.Zero, Vector.Max(accumulatedImpulses.Vertical + verticalCorrectiveImpulse, -maximumVerticalImpulse));
        verticalCorrectiveImpulse = accumulatedImpulses.Vertical - previousVerticalAccumulatedImpulse;

        ApplyVerticalImpulse(basis, verticalAngularJacobianA, verticalAngularJacobianB, verticalCorrectiveImpulse, inertiaA, inertiaB, ref velocityA, ref velocityB);
    }


    public static bool RequiresIncrementalSubstepUpdates => true;
    public static void IncrementallyUpdateForSubstep(in Vector<float> dt, in BodyVelocityWide velocityA, in BodyVelocityWide velocityB, ref DynamicCharacterMotionPrestep prestep)
    {
        //Since collision detection doesn't run for every substep, we approximate the change in depth for the vertical motion constraint by integrating the velocity along the support normal.
        //This is pretty subtle. If you disable it entirely (return false from "RequiresIncrementalSubstepUpdates" above), you might not even notice.
        //If you disable the vertical motion constraint, then it can definitely be disabled.

        //Any movement of the character or its support along N results in a change in the vertical motion constraint's perception of depth.
        //estimatedPenetrationDepthChange = dot(normal, velocityDtA.Linear + velocityDtA.Angular x contactOffsetA) - dot(normal, velocityDtB.Linear + velocityDtB.Angular x contactOffsetB)
        Vector3Wide.CrossWithoutOverlap(velocityA.Angular, prestep.OffsetFromCharacter, out var wxra);
        Vector3Wide.Add(wxra, velocityA.Linear, out var contactVelocityA);

        var normal = QuaternionWide.TransformUnitY(prestep.SurfaceBasis);
        Vector3Wide.CrossWithoutOverlap(velocityB.Angular, prestep.OffsetFromSupport, out var wxrb);
        Vector3Wide.Add(wxrb, velocityB.Linear, out var contactVelocityB);
        Vector3Wide.Subtract(contactVelocityA, contactVelocityB, out var contactVelocityDifference);
        Vector3Wide.Dot(normal, contactVelocityDifference, out var estimatedDepthChangeVelocity);
        prestep.Depth -= estimatedDepthChangeVelocity * dt;
    }
}