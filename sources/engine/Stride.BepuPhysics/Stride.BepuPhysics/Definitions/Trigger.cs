// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Definitions.Contacts;
using Stride.Core;

namespace Stride.BepuPhysics.Definitions;

public delegate void TriggerDelegate(CollidableComponent @this, CollidableComponent other);

/// <summary>
/// A contact event handler without collision response, which runs delegates on enter and exit
/// </summary>
[DataContract]
public class Trigger : IContactHandler
{
    public bool NoContactResponse => true;
    public event TriggerDelegate? OnEnter, OnLeave;

    void IContactHandler.OnStartedTouching<TManifold>(ContactData<TManifold> contactData) => OnStartedTouching(contactData);
    void IContactHandler.OnStoppedTouching<TManifold>(ContactData<TManifold> contactData) => OnStoppedTouching(contactData);

    /// <inheritdoc cref="IContactHandler.OnStartedTouching{TManifold}"/>
    protected void OnStartedTouching<TManifold>(ContactData<TManifold> contactData) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        OnEnter?.Invoke(contactData.EventSource, contactData.Other);
    }

    /// <inheritdoc cref="IContactHandler.OnStoppedTouching{TManifold}"/>
    protected void OnStoppedTouching<TManifold>(ContactData<TManifold> contactData) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        OnLeave?.Invoke(contactData.EventSource, contactData.Other);
    }
}
