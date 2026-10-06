using System;
using Stride.Engine;

namespace Stride.Updater.New;

internal sealed class EntityComponentAccessor<T>(UpdatableMember<Entity> parent, UpdatableType<T> type) : UpdatableMember<T> where T : EntityComponent
{
    public override bool SupportsByReference => false;

    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name)
    {
        return type.ResolveProperty(name, this);
    }
    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name)
    {
        return type.ResolveIndexer(name, this);
    }

    public override T GetValue(object instance)
    {
        var entity = parent.GetValue(instance);
        if (entity != null)
        {
            var components = entity.Components;
            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] is T component)
                {
                    return component;
                }
            }
        }
        return null;
    }

    public override ref T GetReference(object instance)
    {
        throw new NotSupportedException();
    }

    public override void SetValue(object instance, T value)
    {
        throw new NotSupportedException();
    }
}
