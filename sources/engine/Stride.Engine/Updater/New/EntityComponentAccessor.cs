using System;
using Stride.Engine;

namespace Stride.Updater.New;

internal sealed class EntityComponentAccessor<T>(string name, UpdatableType<T> type) : UpdatableMember<Entity, T>, IUpdatableMember<EntityComponentAccessor<T>, Entity, T> where T : EntityComponent
{
    public static bool SupportsByReference => false;

    public override string Name => name;

    internal override bool IsIndexer => true;

    public override UpdatableMember<T> CreateProperty(string name) => type.CreateProperty(name);

    public override UpdatableMember<T> CreateIndexer(string name) => type.CreateIndexer(name);

    public static T GetValue(EntityComponentAccessor<T> @this, Entity entity)
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

    public static void SetValue(EntityComponentAccessor<T> @this, Entity entity, T value)
    {
        throw new NotSupportedException("Entity components cannot be replaced through an update accessor.");
    }

    public override unsafe void Update(Entity parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override unsafe void Update(ref Entity parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);
}
