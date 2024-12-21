// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
//  Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;

namespace Stride.BepuPhysics.Navigation.Processors;
public class BepuStaticColliderTrackingProcessor : EntityProcessor<StaticComponent>
{
    public delegate void CollectionChangedEventHandler(StaticComponent component);

    public event CollectionChangedEventHandler ColliderAdded;
    public event CollectionChangedEventHandler ColliderRemoved;

    /// <inheritdoc />
    protected override void OnEntityComponentAdding(Entity entity, StaticComponent component, StaticComponent data)
    {
        ColliderAdded?.Invoke(component);
    }

    /// <inheritdoc />
    protected override void OnEntityComponentRemoved(Entity entity, StaticComponent component, StaticComponent data)
    {
        ColliderRemoved?.Invoke(component);
    }
}
