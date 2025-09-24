// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.CollisionDetection;

namespace Stride.BepuPhysics.Definitions.Contacts;

/// <summary>
/// A set of contacts generated from two collidables
/// </summary>
/// <code>
/// <![CDATA[
/// void OnStartedTouching<TManifold>(Contacts<TManifold> contacts) where TManifold : unmanaged, IContactManifold<TManifold>
/// {
///    foreach (var contact in contacts)
///    {
///        contact.ContactGroup ...
///    }
///    // Or
///    foreach (var group in contacts.Groups)
///    {
///        ...
///    }
/// }
/// ]]>
/// </code>
public struct ContactGroup<TManifold> where TManifold : unmanaged, IContactManifold<TManifold>
{
    /// <summary>
    /// The raw id for the two collidables that generated this contact group
    /// </summary>
    public readonly CollidablePair Pair;

    /// <summary>
    /// <see cref="Pair"/> sorted in a deterministic order
    /// </summary>
    public readonly (uint A, uint B) SortedPair;

    /// <summary>
    /// When <see cref="CollidablePair.A"/> has a <see cref="Stride.BepuPhysics.Definitions.Colliders.CompoundCollider"/>,
    /// this is the index of the collider in that collection which is in contact.
    /// </summary>
    public readonly int ChildIndexA;

    /// <summary>
    /// When <see cref="CollidablePair.B"/> has a <see cref="Stride.BepuPhysics.Definitions.Colliders.CompoundCollider"/>,
    /// this is the index of the collider in that collection which is in contact.
    /// </summary>
    public readonly int ChildIndexB;

    // This is not readonly specifically because we're calling instance method on this
    // object which may cause the JIT to do a copy before each call
    /// <summary>
    /// The manifold associated with this collision
    /// </summary>
    public TManifold Manifold;

    public ContactGroup(ref TManifold manifold, CollidablePair pair, int childIndexA, int childIndexB)
    {
        Pair = pair;
        SortedPair = OrderedPair.Sort(pair);
        Manifold = manifold;
        ChildIndexA = childIndexA;
        ChildIndexB = childIndexB;
    }
}
