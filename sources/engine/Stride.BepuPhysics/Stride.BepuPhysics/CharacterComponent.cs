// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Components;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using NVector3 = System.Numerics.Vector3;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics;

[ComponentCategory("Physics - Bepu")]
public class CharacterComponent : BodyComponent, ISimulationUpdate, IContactHandler
{
    private bool _jumping;

    /// <summary> Base speed applied when moving, measured in units per second </summary>
    public float Speed { get; set; } = 10f;

    /// <summary> Force of the impulse applied when calling <see cref="TryJump"/> </summary>
    [DataAlias("JumpSpeed")]
    public float JumpForce { get; set; } = 10f;

    [DataMemberIgnore]
    public Vector3 Velocity { get; set; }

    [DataMemberIgnore]
    public bool IsGrounded { get; protected set; }

    public bool IsJumping => _jumping;

    /// <summary>
    /// Order is not guaranteed and may change at any moment
    /// </summary>
    [DataMemberIgnore]
    public List<(CollidableComponent Source, Contact Contact)> Contacts { get; } = new();

    public CharacterComponent()
    {
        InterpolationMode = InterpolationMode.Interpolated;
    }

    /// <inheritdoc cref="BodyComponent.AttachInner"/>
    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        base.AttachInner(pose, new BodyInertia { InverseMass = 1f }, shapeIndex);
        FrictionCoefficient = 0f;
        ContactEventHandler = this;
    }

    /// <summary>
    /// Sets the velocity based on <paramref name="direction"/> and <see cref="Speed"/>
    /// </summary>
    /// <remarks>
    /// <paramref name="direction"/> does not have to be normalized;
    /// if the vector passed in has a length of 2, the character will go twice as fast
    /// </remarks>
    public virtual void Move(Vector3 direction)
    {
        // Note that this method should be thread safe, see usage in RecastPhysicsNavigationProcessor
        Velocity = direction * Speed;
    }

    /// <summary>
    /// Try to perform a jump on the next physics tick, will fail when not grounded
    /// </summary>
    public virtual void TryJump()
    {
        _jumping = true;
    }

    /// <summary>
    /// This is called internally right before the physics simulation does a tick
    /// </summary>
    /// <param name="sim"> The simulation this <see cref="ISimulationUpdate"/> is bound to, and the one currently updating </param>
    /// <param name="simTimeStep">The amount of time in seconds since the last simulation</param>
    public virtual void SimulationUpdate(BepuSimulation sim, float simTimeStep)
    {
        Awake = true; // Keep this body active

        var gravity = Simulation!.PoseGravity;

        // Only keep the vertical component from the linear velocity, be it gravity or jump
        LinearVelocity = Velocity + Project(LinearVelocity, gravity);

        if (_jumping && LinearVelocity.Y <= 0)
        {
            if (IsGrounded)
                ApplyLinearImpulse(-Vector3.Normalize(gravity) * JumpForce);
            else
                _jumping = false;
        }
        Gravity = (IsGrounded && Velocity.LengthSquared() == 0f) == false; // Apply gravity only when the character is grounded and standing still to avoid sliding down slopes
    }

    /// <summary>
    /// This is called internally right after the physics simulation does a tick
    /// </summary>
    /// <param name="sim"> The simulation this <see cref="ISimulationUpdate"/> is bound to, and the one currently updating </param>
    /// <param name="simTimeStep">The amount of time in seconds since the last simulation</param>
    public virtual void AfterSimulationUpdate(BepuSimulation sim, float simTimeStep)
    {
        var gravity = Simulation!.PoseGravity;
        IsGrounded = GroundTest(-gravity.ToNumeric()); // Checking for grounded after simulation ran to compute contacts as soon as possible after they are received

        bool downwardForce = Vector3.Dot(gravity, LinearVelocity) >= 0f;

        if (_jumping && downwardForce)
            _jumping = false; // If we have any downward forces we're past the apex of the jump

        if (IsGrounded && Velocity.LengthSquared() == 0f)
            LinearVelocity = ProjectOnPlane(LinearVelocity, gravity); // Clip gravity when standing still on ground, mostly for slopes
        else if (downwardForce == false && _jumping == false)
            LinearVelocity = ProjectOnPlane(LinearVelocity, gravity); // Clip bouncing upward after a collision
    }

    static Vector3 Project(Vector3 vector, Vector3 direction) => direction * Vector3.Dot(vector, direction) / Vector3.Dot(direction, direction);

    static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal) => vector - Project(vector, planeNormal);


    /// <summary>
    /// Returns whether this body is in contact with the ground.
    /// </summary>
    /// <remarks>
    /// Goes through the list of <see cref="Contacts"/> to do so
    /// </remarks>
    /// <param name="groundNormal"> Which direction a surface has to be in to be considered as ground </param>
    /// <param name="threshold">
    /// How close to this direction a supporting contact
    /// has to be for it to be considered as ground.
    /// In the [-1,1] range, where -1 would return true for any given surface we are in contact with,
    /// 0 would return true for a surface that is at most 90 degrees away from <paramref name="groundNormal"/>,
    /// and 1 would return true only when a surface matches <paramref name="groundNormal"/> exactly.
    /// </param>
    protected bool GroundTest(NVector3 groundNormal, float threshold = 0.1f)
    {
        IsGrounded = false;
        if (Simulation == null || Contacts.Count == 0)
            return false;

        foreach (var contact in Contacts)
        {
            if (contact.Source.ContactEventHandler?.NoContactResponse == true)
                continue;

            var contactNormal = contact.Contact.Normal;

            if (NVector3.Dot(groundNormal, contactNormal) >= threshold) // If the body is supported by a contact whose surface is against gravity
            {
                return true;
            }
        }

        return false;
    }

    bool IContactHandler.NoContactResponse => NoContactResponse;
    void IContactHandler.OnStartedTouching<TManifold>(Contacts<TManifold> contacts) => OnStartedTouching(contacts);
    void IContactHandler.OnTouching<TManifold>(Contacts<TManifold> contacts) => OnTouching(contacts);
    void IContactHandler.OnStoppedTouching<TManifold>(Contacts<TManifold> contacts) => OnStoppedTouching(contacts);


    protected bool NoContactResponse => false;

    /// <inheritdoc cref="IContactHandler.OnStartedTouching{TManifold}"/>
    protected virtual void OnStartedTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        foreach (var contact in contacts)
        {
            Contacts.Add((contacts.Other, new Contact
            {
                Normal = contact.Normal,
                Depth = contact.Depth,
                FeatureId = contact.FeatureId,
                Offset = contact.Point - (Vector3)contacts.EventSource.Pose!.Value.Position,
            }));
        }
    }

    /// <inheritdoc cref="IContactHandler.OnTouching{TManifold}"/>
    protected virtual void OnTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        for (int i = Contacts.Count - 1; i >= 0; i--)
        {
            if (Contacts[i].Source == contacts.Other)
                Contacts.SwapRemoveAt(i);
        }

        foreach (var contact in contacts)
        {
            Contacts.Add((contacts.Other, new Contact
            {
                Normal = contact.Normal,
                Depth = contact.Depth,
                FeatureId = contact.FeatureId,
                Offset = contact.Point - (Vector3)contacts.EventSource.Pose!.Value.Position,
            }));
        }
    }

    /// <inheritdoc cref="IContactHandler.OnStoppedTouching{TManifold}"/>
    protected virtual void OnStoppedTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        for (int i = Contacts.Count - 1; i >= 0; i--)
        {
            if (Contacts[i].Source == contacts.Other)
                Contacts.SwapRemoveAt(i);
        }
    }
}
