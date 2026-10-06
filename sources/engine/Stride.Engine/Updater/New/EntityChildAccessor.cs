using System;
using Stride.Engine;

namespace Stride.Updater.New;

internal sealed class EntityChildAccessor(string childName) : UpdatableMember<Entity, Entity>
{
    protected override bool SupportsByReference => false;

    public override string Name => childName;

    public override UpdatableMember<Entity> CreateProperty(string name)
    {
        return EntityType.Instance.CreateProperty(name);
    }

    public override UpdatableMember<Entity> CreateIndexer(string name)
    {
        return EntityType.Instance.CreateIndexer(name);
    }

    protected override Entity GetValue(Entity entity)
    {
        foreach (var child in entity.Transform.Children)
        {
            var childEntity = child.Entity;
            if (childEntity.Name == childName)
            {
                return childEntity;
            }
        }
        return null;
    }

    protected override void SetValue(Entity entity, Entity value)
    {
        throw new NotSupportedException("Entity children cannot be replaced through an update accessor.");
    }
}
