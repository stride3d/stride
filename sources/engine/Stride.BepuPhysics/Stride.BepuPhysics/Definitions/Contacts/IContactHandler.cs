// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.CollisionDetection;

namespace Stride.BepuPhysics.Definitions.Contacts;

public interface IContactHandler
{
    /// <summary>
    /// Whether the object this is attached to should let colliders pass through it
    /// </summary>
    public bool NoContactResponse { get; }

    /// <summary>
    /// Fires the first time a pair is observed to be touching. Touching means that there are contacts with nonnegative depths in the manifold.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="contactData">Data associated with this contact event.</param>
    void OnStartedTouching<TManifold>(ContactData<TManifold> contactData) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }

    /// <summary>
    /// Fires whenever a pair is observed to be touching. Touching means that there are contacts with nonnegative depths in the manifold. Will not fire for sleeping pairs.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="contactData">Data associated with this contact event.</param>
    void OnTouching<TManifold>(ContactData<TManifold> contactData) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }


    /// <summary>
    /// Fires when a pair stops touching. Touching means that there are contacts with nonnegative depths in the manifold.
    /// </summary>
    /// <typeparam name="TManifold">Type of the contact manifold detected.</typeparam>
    /// <param name="contactData">Data associated with this contact event.</param>
    void OnStoppedTouching<TManifold>(ContactData<TManifold> contactData) where TManifold : unmanaged, IContactManifold<TManifold>
    {
    }
}
