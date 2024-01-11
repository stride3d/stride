using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using NVector3 = System.Numerics.Vector3;

namespace Stride.BepuPhysics.Components;

[ComponentCategory("Bepu - Character")]
public class CharacterComponent : BodyContainerComponent, ISimulationUpdate, IContactEventHandler
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
    public new Quaternion Orientation { get; set; }
    [DataMemberIgnore]
    public Vector3 Velocity { get; set; }
    [DataMemberIgnore]
    public bool IsGrounded { get; private set; }

    /// <summary>
    /// Order is not guaranteed and may change at any moment
    /// </summary>
    [DataMemberIgnore]
    public List<(IContainer Source, Contact Contact)> Contacts { get; } = new();


    public override void Start()
    {
        base.Start();

        FrictionCoefficient = 0f;
        BodyInertia = new BodyInertia { InverseMass = 1f };

        ContactEventHandler = this;

        Entity.Add(new DebugInfo(this));
    }

    public void Move(Vector3 direction)
    {
        Velocity = direction * Speed;
    }

    public void Rotate(Quaternion rotation)
    {
        Orientation = rotation;
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

        base.Orientation = Orientation;
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
        IgnoreGlobalGravity = IsGrounded && Velocity.Length() <= 0f;
    }
    private void CheckGrounded()
    {
        IsGrounded = false;
        if (Simulation == null || Contacts.Count == 0)
            return;

        var gravity = Simulation.PoseGravity.ToNumericVector();
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

    void IContactEventHandler.OnStoppedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
    {
        var sim = Simulation;
        if (sim == null)
        {
            Contacts.Clear();
            return;
        }

        var containerA = pair.A.GetContainerFromCollidable(sim);
        var containerB = pair.B.GetContainerFromCollidable(sim);
        if (containerA == null || containerB == null)
        {
            return;
        }
        var otherContainer = this == containerA ? containerB : containerA;
        for (int i = Contacts.Count - 1; i >= 0; i--)
        {
            if (Contacts[i].Source == otherContainer)
                Contacts.SwapRemoveAt(i);
        }
    }
    void IContactEventHandler.OnStartedTouching<TManifold>(CollidableReference eventSource, CollidablePair pair, ref TManifold contactManifold, int contactIndex)
    {
        var sim = Simulation;
        if (sim == null)
            throw new InvalidOperationException("Received new contacts in a container that's not part of the simulation");

        var containerA = pair.A.GetContainerFromCollidable(sim);
        var containerB = pair.B.GetContainerFromCollidable(sim);
        if (containerA == null || containerB == null)
        {
            return;
        }
        var otherContainer = this == containerA ? containerB : containerA;

        contactManifold.GetContact(contactIndex, out var contact);
        contact.Offset = contact.Offset + containerA.Entity.Transform.GetWorldPos().ToNumericVector() + containerA.CenterOfMass.ToNumericVector();
        Contacts.Add((otherContainer, contact));
    }

    class DebugInfo : SyncScript
    {
        readonly CharacterComponent _character;
        public DebugInfo(CharacterComponent character)
        {
            _character = character;
        }

        public override void Update()
        {
            DebugText.Print($"Mouse delta : {Input.MouseDelta}", new Int2(50, 950));
            DebugText.Print($"Velocity : {_character.Velocity}", new Int2(50, 975));
            DebugText.Print($"Orientation : {_character.Orientation}", new Int2(50, 1000));
            DebugText.Print($"IsGrounded : {_character.IsGrounded}", new Int2(50, 1025));
            DebugText.Print($"ContactPoints count : {_character.Contacts.Count}", new Int2(50, 1050));
        }
    }
}


