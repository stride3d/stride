// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.CollisionDetection;
using BepuUtilities;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Systems.Characters;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using NVector3 = System.Numerics.Vector3;

namespace Stride.BepuPhysics;

[ComponentCategory("Physics - Bepu")]
public class CharacterComponent : CharacterComponentAbstract
{
    private Vector3 _velocity;

    /// <summary> Base speed applied when moving, measured in units per second </summary>
    public float Speed { get; set; } = 10f;

    /// <summary> Maximum force the character can apply to move along the supporting surface </summary>
    public float MaximumHorizontalForce { get; set; } = 200;

    /// <summary> Force of the impulse applied when calling <see cref="TryJump"/> </summary>
    [DataAlias("JumpSpeed")]
    public float JumpForce { get; set; } = 10f;

    /// <summary> Maximum force the character can apply to glue itself to the supporting surface </summary>
    public float MaximumVerticalForce { get; set; } = 1000;

    /// <summary> The maximum slope angle in degrees that the character can treat as a support </summary>
    [DataMemberRange(1, 89, 1, 10, 1)]
    public float SlopeAngle { get; set; } = 50;

    /// <summary> Threshold under which a contact is accepted as a support surface if the normal allows it </summary>
    public float MinimumSupportDepth { get; set; } = 0.005f;

    /// <summary> Threshold under which the previous supporting surfaces' contact is still considered a support </summary>
    public float MinimumSupportContinuationDepth { get; set; } = 0.1f;

    /// <summary> Ratio of <see cref="MaximumHorizontalForce"/> to use when not on a supporting surface </summary>
    [DataMemberRange(0, 1, 0.1, 0.25, 3)]
    public float AirControlForceScale { get; set; } = 0.2f;

    /// <summary> Ratio of <see cref="Speed"/> to use when not on a supporting surface </summary>
    [DataMemberRange(0, 1, 0.1, 0.25, 3)]
    public float AirControlScale { get; set; } = 0.2f;

    /// <summary> The direction local to <see cref="BodyComponent.Orientation"/> used when jumping or finding a supporting surface </summary>
    public Vector3 LocalUp { get; set; } = new(0, 1, 0);

    [DataMemberIgnore, Obsolete($"Use {nameof(MoveVector)} instead")]
    public Vector3 Velocity
    {
        get => _velocity;
        set
        {
            _velocity = value;

            var inv = Quaternion.Invert(GlobalBasis);
            value = inv * value;
            MoveVector = new Vector2(value.X, value.Z) / Speed;
        }
    }

    /// <summary>
    /// The current movement direction of this component, it is local to its <see cref="BodyComponent.Orientation"/>
    /// where +X is left, -X is right, +Y is forward and -Y is backward. This is similar to how the arrow Gizmo in the editor is laid out.
    /// </summary>
    /// <remarks>
    /// The input is scaled by <see cref="Speed"/> before usage<br/>
    /// <paramref name="value"/> does not have to be normalized;
    /// if the vector passed in has a length of 2, the character will go twice as fast
    /// </remarks>
    public Vector2 MoveVector
    {
        get;
        set
        {
            field = value;
            _velocity = GlobalBasis * new Vector3(value.X, 0, value.Y) * Speed;
        }
    }

    [DataMemberIgnore]
    public bool IsGrounded
    {
        get
        {
            if (BodyReference is { } bodyHandle)
            {
                Debug.Assert(Simulation is not null);
                ref var character = ref Simulation.Characters.GetCharacterByBodyHandle(bodyHandle);
                return character.Supported;
            }

            return false;
        }
        [Obsolete($"{nameof(IsGrounded)} is now handled internally")]
        protected set
        {
        }
    }

    public bool IsJumping
    {
        get
        {
            if (BodyReference is { } bodyHandle)
            {
                Debug.Assert(Simulation is not null);
                ref var character = ref Simulation.Characters.GetCharacterByBodyHandle(bodyHandle);
                return character.TryJump;
            }

            return false;
        }
    }

    /// <summary>
    /// Order is not guaranteed and may change at any moment
    /// </summary>
    [DataMemberIgnore, Obsolete($"Contacts are no longer collected, add your own {nameof(ContactEventHandler)}")]
    public List<(CollidableComponent Source, Contact Contact)> Contacts { get; } = new();

    private Quaternion GlobalBasis => Quaternion.LookRotation(Orientation * Vector3.UnitZ, Orientation * LocalUp);

    public CharacterComponent()
    {
        InterpolationMode = InterpolationMode.Interpolated;
    }

    /// <summary>
    /// Sets the velocity based on <paramref name="direction"/> and <see cref="Speed"/>
    /// </summary>
    /// <remarks>
    /// <paramref name="direction"/> does not have to be normalized;
    /// if the vector passed in has a length of 2, the character will go twice as fast
    /// </remarks>
    [Obsolete($"Use {nameof(MoveVector)} instead")]
    public virtual void Move(Vector3 direction)
    {
        // Note that this method should be thread safe, see usage in RecastPhysicsNavigationProcessor
        var inv = Quaternion.Invert(GlobalBasis);
        direction = inv * direction;
        MoveVector = new Vector2(direction.X, direction.Z);
    }

    /// <summary>
    /// Try to perform a jump on the next physics tick, will fail when not grounded
    /// </summary>
    public virtual void TryJump()
    {
        if (BodyReference is { } bodyHandle)
        {
            Debug.Assert(Simulation is not null);
            ref var character = ref Simulation.Characters.GetCharacterByBodyHandle(bodyHandle);
            character.TryJump = true;
        }
    }

    /// <inheritdoc/>
    protected override void SimulationUpdate(BepuSimulation sim, float simTimeStep, ref InternalCharacterData character, in BodyReference characterBody, out bool wakeupBody)
    {
        var viewDirection = (NVector3)(Orientation * Vector3.UnitZ);

        var newTargetVelocity = new System.Numerics.Vector2(MoveVector.X, MoveVector.Y) * Speed;
        //Modifying the character's raw data does not automatically wake the character up, so we do so explicitly if necessary.
        //If you don't explicitly wake the character up, it won't respond to the changed motion goals.
        //(You can also specify a negative deactivation threshold in the BodyActivityDescription to prevent the character from sleeping at all.)
        if (!characterBody.Awake)
        {
            wakeupBody = (character.TryJump && character.Supported)
                         || newTargetVelocity != character.TargetVelocity
                         || (newTargetVelocity != System.Numerics.Vector2.Zero && character.ViewDirection != viewDirection);
        }
        else
        {
            wakeupBody = false;
        }

        character.ViewDirection = viewDirection;
        character.TargetVelocity = newTargetVelocity;
        character.LocalUp = LocalUp;
        character.JumpVelocity = JumpForce;
        character.MaximumHorizontalForce = MaximumHorizontalForce;
        character.MaximumVerticalForce = MaximumVerticalForce;
        character.CosMaximumSlope = MathF.Cos(MathUtil.DegreesToRadians(SlopeAngle));
        character.MinimumSupportDepth = -MinimumSupportDepth;
        character.MinimumSupportContinuationDepth = -MinimumSupportContinuationDepth;

        //The character's motion constraints aren't active while the character is in the air, so if we want air control, we'll need to apply it ourselves.
        //(You could also modify the constraints to do this, but the robustness of solved constraints tends to be a lot less important for air control.)
        //There isn't any one 'correct' way to implement air control- it's a nonphysical gameplay thing, and this is just one way to do it.
        //Note that this permits accelerating along a particular direction, and never attempts to slow down the character.
        //This allows some movement quirks common in some game character controllers.
        //Consider what happens if, starting from a standstill, you accelerate fully along X, then along Z- your full velocity magnitude will be sqrt(2) * maximumAirSpeed.
        //Feel free to try alternative implementations. Again, there is no one correct approach.
        if (!character.Supported && newTargetVelocity.LengthSquared() > 0)
        {
            QuaternionEx.Transform(character.LocalUp, characterBody.Pose.Orientation, out var characterUp);
            var characterLeft = Vector3.Cross(characterUp, character.ViewDirection);
            var rightLengthSquared = characterLeft.LengthSquared();
            if (rightLengthSquared > 1e-10f)
            {
                characterLeft /= MathF.Sqrt(rightLengthSquared);
                var characterForward = Vector3.Cross(characterLeft, characterUp);
                var worldMovementDirection = characterLeft * newTargetVelocity.X + characterForward * newTargetVelocity.Y;
                var currentVelocity = Vector3.Dot(characterBody.Velocity.Linear, worldMovementDirection);
                //We'll arbitrarily set air control to be a fraction of supported movement's speed/force.
                var airAccelerationDt = characterBody.LocalInertia.InverseMass * character.MaximumHorizontalForce * AirControlForceScale * simTimeStep;
                var maximumAirSpeed = Speed * AirControlScale;
                var targetVelocity = MathF.Min(currentVelocity + airAccelerationDt, maximumAirSpeed);
                //While we shouldn't allow the character to continue accelerating in the air indefinitely, trying to move in a given direction should never slow us down in that direction.
                var velocityChangeAlongMovementDirection = MathF.Max(0, targetVelocity - currentVelocity);
                characterBody.Velocity.Linear += worldMovementDirection * velocityChangeAlongMovementDirection;
                Debug.Assert(characterBody.Awake || wakeupBody, "Velocity changes don't automatically update objects; the character should have already been woken up before applying air control.");
            }
        }
    }

    /// <inheritdoc/>
    protected override void AfterSimulationUpdate(BepuSimulation sim, float simTimeStep, ref InternalCharacterData characterData)
    {
    }

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
    [Obsolete($"Contacts are no longer collected, add your own {nameof(ContactEventHandler)}")]
    protected bool GroundTest(NVector3 groundNormal, float threshold = 0f)
    {
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


    [Obsolete($"Contacts are no longer collected, add your own {nameof(ContactEventHandler)}")]
    protected bool NoContactResponse => false;

    /// <inheritdoc cref="IContactHandler.OnStartedTouching{TManifold}"/>
    [Obsolete($"Contacts are no longer collected, add your own {nameof(ContactEventHandler)}")]
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
    [Obsolete($"Contacts are no longer collected, add your own {nameof(ContactEventHandler)}")]
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
    [Obsolete($"Contacts are no longer collected, add your own {nameof(ContactEventHandler)}")]
    protected virtual void OnStoppedTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        for (int i = Contacts.Count - 1; i >= 0; i--)
        {
            if (Contacts[i].Source == contacts.Other)
                Contacts.SwapRemoveAt(i);
        }
    }
}
