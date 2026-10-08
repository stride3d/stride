using System;
using Stride.Engine;

namespace Stride.Updater.New;

internal sealed class EntityChildAccessor(string childName) : UpdatableMember<Entity, Entity>, IUpdatableMember<EntityChildAccessor, Entity, Entity>
{
    public static bool SupportsByReference => false;

    public override string Name => childName;

    public override UpdatableMember<Entity> CreateProperty(string name)
    {
        return EntityType.Instance.CreateProperty(name);
    }

    public override UpdatableMember<Entity> CreateIndexer(string name)
    {
        return EntityType.Instance.CreateIndexer(name);
    }

    public static Entity GetValue(EntityChildAccessor @this, Entity entity)
    {
        foreach (var child in entity.Transform.Children)
        {
            var childEntity = child.Entity;
            if (childEntity.Name == @this.Name)
            {
                return childEntity;
            }
        }
        return null;
    }

    public static void SetValue(EntityChildAccessor @this, Entity entity, Entity value)
    {
        throw new NotSupportedException("Entity children cannot be replaced through an update accessor.");
    }

    public override unsafe void Update(Entity parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override unsafe void Update(ref Entity parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);
}
