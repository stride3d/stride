// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.CollisionDetection;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions.Contacts;

/// <summary>
/// Enumerate over this structure to get individual contacts
/// </summary>
/// <code>
/// <![CDATA[
/// void OnStartedTouching<TManifold>(ContactData<TManifold> contactData) where TManifold : unmanaged, IContactManifold<TManifold>
/// {
///    foreach (var contact in contactData)
///    {
///        contact.Normal ...
///    }
/// }
/// ]]>
/// </code>
public readonly ref struct ContactData<TManifold> where TManifold : unmanaged, IContactManifold<TManifold>
{
    /// <summary>
    /// The collidable which is bound to this <see cref="IContactHandler"/>
    /// </summary>
    public CollidableComponent EventSource { get; init; }

    /// <summary>
    /// The other collidable
    /// </summary>
    public CollidableComponent Other { get; init; }

    /// <summary>
    /// The simulation this contact occured in
    /// </summary>
    public BepuSimulation Simulation { get; init; }

    /// <summary>
    /// The raw contact manifold
    /// </summary>
    /// <remarks>
    /// Make sure that you understand and handle <see cref="FlippedManifold"/> before using this property
    /// </remarks>
    public TManifold Manifold { get; init; }

    /// <summary>
    /// Whether the data within <see cref="Manifold"/> should be treated as flipped from <see cref="EventSource"/>'s perspective,
    /// e.g.: the normals in <see cref="Manifold"/> should be inverted.
    /// Use <see cref="GetEnumerator"/> instead.
    /// </summary>
    public bool FlippedManifold { get; init; }

    /// <summary>
    /// When <see cref="EventSource"/> has a <see cref="Stride.BepuPhysics.Definitions.Colliders.CompoundCollider"/>,
    /// this is the index of the collider in that collection which <see cref="Other"/> collided with.
    /// </summary>
    public int ChildIndexSource { get; init; }

    /// <summary>
    /// When <see cref="Other"/> has a <see cref="Stride.BepuPhysics.Definitions.Colliders.CompoundCollider"/>,
    /// this is the index of the collider in that collection which <see cref="EventSource"/> collided with.
    /// </summary>
    public int ChildIndexOther { get; init; }

    /// <inheritdoc cref="ContactData{TManifold}"/>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// The enumerator for <see cref="ContactData{TManifold}"/>
    /// </summary>
    /// <inheritdoc cref="ContactData{TManifold}"/>
    public ref struct Enumerator(ContactData<TManifold> data)
    {
        private int _index = -1;
        private ContactData<TManifold> _data = data;

        public bool MoveNext()
        {
            while (_index + 1 < _data.Manifold.Count)
            {
                _index += 1;
                if (_data.Manifold.GetDepth(_index) >= 0)
                    return true;
            }

            return false;
        }

        public Contact Current => new(_index, _data);
    }

    /// <summary>
    /// An individual contact
    /// </summary>
    /// <inheritdoc cref="ContactData{TManifold}"/>
    public readonly ref struct Contact(int index, ContactData<TManifold> data)
    {
        public int Index { get; } = index;
        public ContactData<TManifold> Data { get; } = data;

        /// <summary>
        /// The contact's normal, oriented based on <see cref="ContactData{TManifold}.EventSource"/>
        /// </summary>
        public Vector3 Normal => Data.FlippedManifold ? -Data.Manifold.GetNormal(Index) : Data.Manifold.GetNormal(Index);

        /// <summary> How far the two collidables intersect </summary>
        public float Depth => Data.Manifold.GetDepth(Index);

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
                if (Data.FlippedManifold)
                    return Data.Other.Pose!.Value.Position + Data.Manifold.GetOffset(Index);
                return Data.EventSource.Pose!.Value.Position + Data.Manifold.GetOffset(Index);
            }
        }

        /// <summary> Gets the feature id associated with this contact </summary>
        public int FeatureId => Data.Manifold.GetFeatureId(Index);
    }
}
