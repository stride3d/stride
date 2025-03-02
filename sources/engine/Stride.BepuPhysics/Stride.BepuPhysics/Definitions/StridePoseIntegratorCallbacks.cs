// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using BepuPhysics;
using BepuUtilities;


namespace Stride.BepuPhysics.Definitions;

internal struct StridePoseIntegratorCallbacks(CollidableProperty<MaterialProperties> CollidableMaterials) : IPoseIntegratorCallbacks
{
    private Bodies _bodies = null!;

    private Vector3Wide _gravityWideDt = default;
    private Vector<float> _linearDampingDt = default;
    private Vector<float> _angularDampingDt = default;

    public bool UsePerBodyAttributes { get; set; } = true;

    /// <summary>
    /// Gravity to apply to dynamic bodies in the simulation.
    /// </summary>
    public Vector3 Gravity { get; set; } = new(0, -9.8f, 0);
    /// <summary>
    /// Fraction of dynamic body linear velocity to remove per unit of time. Values range from 0 to 1. 0 is fully undamped, while values very close to 1 will remove most velocity.
    /// </summary>
    public float LinearDamping { get; set; } = 0.05f;
    /// <summary>
    /// Fraction of dynamic body angular velocity to remove per unit of time. Values range from 0 to 1. 0 is fully undamped, while values very close to 1 will remove most velocity.
    /// </summary>
    public float AngularDamping { get; set; } = 0.05f;


    /// <summary>
    /// Gets how the pose integrator should handle angular velocity integration.
    /// </summary>
    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

    /// <summary>
    /// Gets whether the integrator should use substepping for unconstrained bodies when using a substepping solver.
    /// If true, unconstrained bodies will be integrated with the same number of substeps as the constrained bodies in the solver.
    /// If false, unconstrained bodies use a single step of length equal to the dt provided to Simulation.Timestep.
    /// </summary>
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;

    /// <summary>
    /// Gets whether the velocity integration callback should be called for kinematic bodies.
    /// If true, IntegrateVelocity will be called for bundles including kinematic bodies.
    /// If false, kinematic bodies will just continue using whatever velocity they have set.
    /// Most use cases should set this to false.
    /// </summary>
    public readonly bool IntegrateVelocityForKinematics => false;

    public void Initialize(Simulation simulation)
    {
        _bodies = simulation.Bodies;
    }

    /// <summary>
    /// Callback invoked ahead of dispatches that may call into <see cref="IntegrateVelocity"/>.
    /// It may be called more than once with different values over a frame. For example, when performing bounding box prediction, velocity is integrated with a full frame time step duration.
    /// During substepped solves, integration is split into substepCount steps, each with fullFrameDuration / substepCount duration.
    /// The final integration pass for unconstrained bodies may be either fullFrameDuration or fullFrameDuration / substepCount, depending on the value of AllowSubstepsForUnconstrainedBodies.
    /// </summary>
    /// <param name="dt">Current integration time step duration.</param>
    /// <remarks>This is typically used for precomputing anything expensive that will be used across velocity integration.</remarks>
    public void PrepareForIntegration(float dt)
    {
        //No reason to recalculate gravity * dt for every body; just cache it ahead of time.
        //Since these callbacks don't use per-body damping values, we can precalculate everything.
        _linearDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - LinearDamping, 0, 1), dt));
        _angularDampingDt = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - AngularDamping, 0, 1), dt));
        _gravityWideDt = Vector3Wide.Broadcast(Gravity * dt);
    }

    /// <summary>
    /// Callback for a bundle of bodies being integrated.
    /// </summary>
    /// <param name="bodyIndices">Indices of the bodies being integrated in this bundle.</param>
    /// <param name="position">Current body positions.</param>
    /// <param name="orientation">Current body orientations.</param>
    /// <param name="localInertia">Body's current local inertia.</param>
    /// <param name="integrationMask">Mask indicating which lanes are active in the bundle. Active lanes will contain 0xFFFFFFFF, inactive lanes will contain 0.</param>
    /// <param name="workerIndex">Index of the worker thread processing this bundle.</param>
    /// <param name="dt">Durations to integrate the velocity over. Can vary over lanes.</param>
    /// <param name="velocity">Velocity of bodies in the bundle. Any changes to lanes which are not active by the integrationMask will be discarded.</param>
    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
    {

        //This is a handy spot to implement things like position dependent gravity or per-body damping.
        //This implementation uses a single damping value for all bodies that allows it to be precomputed.
        //We don't have to check for kinematics; IntegrateVelocityForKinematics returns false, so we'll never see them in this callback.
        //Note that these are SIMD operations and "Wide" types. There are Vector<float>.Count lanes of execution being evaluated simultaneously.
        //The types are laid out in array-of-structures-of-arrays (AOSOA) format. That's because this function is frequently called from vectorized contexts within the solver.
        //Transforming to "array of structures" (AOS) format for the callback and then back to AOSOA would involve a lot of overhead, so instead the callback works on the AOSOA representation directly.
        if (UsePerBodyAttributes)
        {
            Span<float> gravitySpan = stackalloc float[Vector<float>.Count];
            for (int bundleSlotIndex = 0; bundleSlotIndex < Vector<int>.Count; ++bundleSlotIndex)
            {
                var bodyIndex = bodyIndices[bundleSlotIndex];
                //Not every slot in the SIMD vector is guaranteed to be filled.
                //The integration mask tells us which ones are active in a way that's convenient for vectorized operations, but the bodyIndex for empty lanes will also be -1.
                if (bodyIndex >= 0 && bodyIndex < _bodies.ActiveSet.Count)
                {
                    var bodyHandle = _bodies.ActiveSet.IndexToHandle[bodyIndex];
                    gravitySpan[bundleSlotIndex] = CollidableMaterials[bodyHandle].Gravity ? 1f : 0f;
                }
                else if (bodyIndex >= 0)
                {
                    Debug.Assert(false); //no longer occuring :)
                    gravitySpan[bundleSlotIndex] = 0f;
                }
            }

            var GravityVec = new Vector<float>(gravitySpan);


            velocity.Linear = (velocity.Linear + (_gravityWideDt * GravityVec)) * _linearDampingDt;
            velocity.Angular = velocity.Angular * _angularDampingDt;

            //Probably not needed.
            //velocity.Linear *= Vector3Wide.Broadcast(new Vector3(1, 1, 0));
            //velocity.Angular *= Vector3Wide.Broadcast(new Vector3(0, 0, 1));
            //position *= Vector3Wide.Broadcast(new Vector3(1, 1, 0));
            ////Quaternion.RotationYawPitchRoll(ref bodyRot, out var yaw, out var pitch, out var roll);
            ////body.Orientation = Quaternion.RotationYawPitchRoll(0, 0, roll);
            //QuaternionWide.Broadcast(new Quaternion(0, 0, 1, 0), out var res); //that will not work (: it's placeholder
            //orientation *= res;
        }
        else
        {
            velocity.Linear = (velocity.Linear + _gravityWideDt) * _linearDampingDt;
            velocity.Angular = velocity.Angular * _angularDampingDt;
        }

    }
}
