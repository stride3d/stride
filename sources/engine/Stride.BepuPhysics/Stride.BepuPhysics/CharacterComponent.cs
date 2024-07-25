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

[ComponentCategory("Bepu")]
public class CharacterComponent : BodyComponent, ISimulationUpdate, IContactEventHandler
{
    private bool _tryJump;

    /// <summary>
    /// Movement speed
    /// </summary>
    public float Speed { get; set; } = 10f;
    /// <summary>
    /// Jump force
    /// </summary>
    public float JumpSpeed { get; set; } = 10f;

    [DataMemberIgnore]
    public Vector3 Velocity { get; set; }
    [DataMemberIgnore]
    public bool IsGrounded { get; private set; }

    /// <summary>
    /// Order is not guaranteed and may change at any moment
    /// </summary>
    [DataMemberIgnore]
    public List<(CollidableComponent Source, Contact Contact)> Contacts { get; } = new();

    public CharacterComponent()
    {
        InterpolationMode = InterpolationMode.Interpolated;
    }

    protected override void AttachInner(NRigidPose pose, BodyInertia shapeInertia, TypedIndex shapeIndex)
    {
        #warning Norbo: validate whether we can remove the setter for BodyInertia below by feeding it in place of shapeIntertia here ?
        base.AttachInner(pose, shapeInertia, shapeIndex);
        FrictionCoefficient = 0f;
        BodyInertia = new BodyInertia { InverseMass = 1f };
        ContactEventHandler = this;
    }

    public void Move(Vector3 direction)
    {
        // Note that this method should be thread safe, see usage in RecastPhysicsNavigationProcessor
        Velocity = direction * Speed;
    }

    /// <summary>
    /// Will jump if grounded
    /// </summary>
    public void TryJump()
    {
        _tryJump = true;
    }

    public void SimulationUpdate(float simTimeStep)
    {
        Awake = true; // Keep this body active

        LinearVelocity = new Vector3(Velocity.X, LinearVelocity.Y, Velocity.Z);

        if (_tryJump)
        {
            if (IsGrounded)
                ApplyLinearImpulse(Vector3.UnitY * JumpSpeed);
            _tryJump = false;
        }
    }
    public void AfterSimulationUpdate(float simTimeStep)
    {
        CheckGrounded(); // Checking for grounded after simulation ran to compute contacts as soon as possible after they are received
        // If there is no input from the player and we are grounded, ignore gravity to prevent sliding down the slope we might be on
        // Do not ignore if there is any input to ensure we stick to the surface as much as possible while moving down the slope
        Gravity = !IsGrounded || Velocity.Length() > 0f;
    }
    private void CheckGrounded()
    {
        IsGrounded = false;
        if (Simulation == null || Contacts.Count == 0)
            return;

        var gravity = Simulation.PoseGravity.ToNumeric();
        foreach (var contact in Contacts)
        {
            var contactNormal = contact.Contact.Normal;

            if (NVector3.Dot(gravity, contactNormal) < 0) // If the body is supported by a contact whose surface is against gravity
            {
                IsGrounded = true;
                return;
            }
        }
    }

    bool IContactEventHandler.NoContactResponse => NoContactResponse;
    void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation) => OnStartedTouching(eventSource, other, ref contactManifold, contactIndex, bepuSimulation);
    void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation) => OnStoppedTouching(eventSource, other, ref contactManifold, contactIndex, bepuSimulation);


    protected bool NoContactResponse => false;

    protected void OnStartedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        var otherCollidable = bepuSimulation.GetComponent(other);
        contactManifold.GetContact(contactIndex, out var contact);
        contact.Offset = contact.Offset + Entity.Transform.WorldMatrix.TranslationVector.ToNumeric() + CenterOfMass.ToNumeric();
        Contacts.Add((otherCollidable, contact));
    }

    protected void OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidableReference other, ref TManifold contactManifold, int contactIndex, BepuSimulation bepuSimulation) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        var otherCollidable = bepuSimulation.GetComponent(other);
        for (int i = Contacts.Count - 1; i >= 0; i--)
        {
            if (Contacts[i].Source == otherCollidable)
                Contacts.SwapRemoveAt(i);
        }
    }
}


