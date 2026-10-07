// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions.Contacts;

/// <summary>
/// An individual contact
/// </summary>
/// <inheritdoc cref="Contacts{TManifold}"/>
public ref struct Contact<TManifold> where TManifold : unmanaged, IContactManifold<TManifold>
{
    /// <summary>
    /// The index used when reading into this group's manifold to retrieve this contact
    /// </summary>
    public readonly int Index;

    /// <summary>
    /// The contact info pair this contact is a part of
    /// </summary>
    public readonly Contacts<TManifold> Contacts;

    // This is not readonly specifically because we're calling instance method on this
    // object which may cause the JIT to do a copy before each call
    /// <summary>
    /// The group this contact is a part of
    /// </summary>
    public ContactGroup<TManifold> ContactGroup;

    /// <summary> Whether <see cref="Contacts{TManifold}.EventSource"/> is the A side of this contact's group </summary>
    /// <remarks> Can differ between groups when either collidable spans multiple bodies </remarks>
    internal readonly bool IsSourceA;

    internal Contact(int index, Contacts<TManifold> contacts, in ContactGroup<TManifold> contactGroup)
    {
        Index = index;
        Contacts = contacts;
        ContactGroup = contactGroup;
        IsSourceA = ReferenceEquals(contacts.Simulation.GetComponent(contactGroup.Pair.A), contacts.EventSource);
    }

    /// <summary> How far the two collidables intersect </summary>
    public float Depth => ContactGroup.Manifold.GetDepth(Index);

    /// <summary> Gets the feature id associated with this contact </summary>
    public int FeatureId => ContactGroup.Manifold.GetFeatureId(Index);

    /// <summary>
    /// The normal on <see cref="Contacts{TManifold}.Other"/>'s surface. Points from <see cref="Contacts{TManifold}.Other"/> towards <see cref="Contacts{TManifold}.EventSource"/>'s surface
    /// </summary>
    public Vector3 Normal => IsSourceA ? ContactGroup.Manifold.GetNormal(Index) : -ContactGroup.Manifold.GetNormal(Index);

    /// <summary>
    /// When <see cref="Contacts{TManifold}.EventSource"/> has a <see cref="Stride.BepuPhysics.Definitions.Colliders.CompoundCollider"/>,
    /// this is the index of the collider in that collection which <see cref="Contacts{TManifold}.Other"/> collided with.
    /// </summary>
    public int SourceChildIndex => IsSourceA ? ContactGroup.ChildIndexA : ContactGroup.ChildIndexB;

    /// <summary>
    /// When <see cref="Contacts{TManifold}.Other"/> has a <see cref="Stride.BepuPhysics.Definitions.Colliders.CompoundCollider"/>,
    /// this is the index of the collider in that collection which <see cref="Contacts{TManifold}.EventSource"/> collided with.
    /// </summary>
    public int OtherChildIndex => IsSourceA ? ContactGroup.ChildIndexB : ContactGroup.ChildIndexA;

    /// <summary> The position at which the contact occured </summary>
    /// <remarks> This may not be accurate if they separated within this tick, or when you removed either of them from the simulation within this scope </remarks>
    public Vector3 Point
    {
        get
        {
            // Pose! is not safe as the component may not be part of the physics simulation anymore, but there's no straightforward fix for this;
            // We collect contacts during the physics tick, after the tick, we send contact events.
            // At that point, both objects may not be at the same position they made contact at,
            // so we can't make this more robust by storing the position they were at on contact within the physics tick.
            var componentA = IsSourceA ? Contacts.EventSource : Contacts.Other;
            if (componentA.CollidableCount > 1)
            {
                // Offsets are relative to the body that touched, not to the pose of a collidable spanning multiple bodies
                var a = ContactGroup.Pair.A;
                var simulation = Contacts.Simulation.Simulation;
                var origin = a.Mobility == CollidableMobility.Static ? simulation.Statics[a.StaticHandle].Pose.Position : simulation.Bodies[a.BodyHandle].Pose.Position;
                return origin.ToStride() + ContactGroup.Manifold.GetOffset(Index);
            }

            return componentA.Pose!.Value.Position + ContactGroup.Manifold.GetOffset(Index);
        }
    }
}
