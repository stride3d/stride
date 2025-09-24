// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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

    internal Contact(int index, Contacts<TManifold> contacts, in ContactGroup<TManifold> contactGroup)
    {
        Index = index;
        Contacts = contacts;
        ContactGroup = contactGroup;
    }

    /// <summary> How far the two collidables intersect </summary>
    public float Depth => ContactGroup.Manifold.GetDepth(Index);

    /// <summary> Gets the feature id associated with this contact </summary>
    public int FeatureId => ContactGroup.Manifold.GetFeatureId(Index);

    /// <summary>
    /// The contact's normal, oriented based on <see cref="Contacts{TManifold}.EventSource"/>
    /// </summary>
    public Vector3 Normal => Contacts.IsSourceOriginalA ? -ContactGroup.Manifold.GetNormal(Index) : ContactGroup.Manifold.GetNormal(Index);

    /// <summary>
    /// When <see cref="Contacts{TManifold}.EventSource"/> has a <see cref="Stride.BepuPhysics.Definitions.Colliders.CompoundCollider"/>,
    /// this is the index of the collider in that collection which <see cref="Contacts{TManifold}.Other"/> collided with.
    /// </summary>
    public int SourceChildIndex => Contacts.IsSourceOriginalA ? ContactGroup.ChildIndexA : ContactGroup.ChildIndexB;

    /// <summary>
    /// When <see cref="Contacts{TManifold}.Other"/> has a <see cref="Stride.BepuPhysics.Definitions.Colliders.CompoundCollider"/>,
    /// this is the index of the collider in that collection which <see cref="Contacts{TManifold}.EventSource"/> collided with.
    /// </summary>
    public int OtherChildIndex => Contacts.IsSourceOriginalA ? ContactGroup.ChildIndexB : ContactGroup.ChildIndexA;

    /// <summary> The position at which the contact occured </summary>
    /// <remarks> This may not be accurate if either collidables are not part of the simulation anymore </remarks>
    public Vector3 Point
    {
        get
        {
            // Pose! is not safe as the component may not be part of the physics simulation anymore, but there's no straightforward fix for this;
            // We collect contacts during the physics tick, after the tick, we send contact events.
            // At that point, both objects may not be at the same position they made contact at,
            // so we can't make this more robust by storing the position they were at on contact within the physics tick.
            return (Contacts.IsSourceOriginalA ? Contacts.EventSource.Pose!.Value.Position : Contacts.Other.Pose!.Value.Position) + ContactGroup.Manifold.GetOffset(Index);
        }
    }
}
