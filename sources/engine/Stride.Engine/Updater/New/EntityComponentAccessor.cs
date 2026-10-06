using System;
using Stride.Engine;

namespace Stride.Updater.New;

internal sealed class EntityComponentAccessor<T>(UpdatableType<T> type) : UpdatableMember<Entity, T> where T : EntityComponent
{
    protected override bool SupportsByReference => false;

    public override string Name => typeof(T).FullName;

    public override UpdatableMember<T> CreateProperty(string name) => type.CreateProperty(name);

    public override UpdatableMember<T> CreateIndexer(string name) => type.CreateIndexer(name);

    protected override T GetValue(Entity entity)
    {
        var components = entity.Components;
        for (int i = 0; i < components.Count; i++)
        {
            if (components[i] is T component)
            {
                return component;
            }
        }
        return null;
    }

    protected override void SetValue(Entity entity, T value)
    {
        throw new NotSupportedException("Entity components cannot be replaced through an update accessor.");
    }
}
