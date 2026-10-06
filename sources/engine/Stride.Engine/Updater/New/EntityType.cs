using System;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Engine;

namespace Stride.Updater.New;

public sealed class EntityType : UpdatableType<Entity>
{
    public static EntityType Instance { get; } = new();

    [ModuleInitializer]
    internal static void Register()
    {
        UpdateEngine.RegisterType(typeof(Entity), Instance);
    }

    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name, UpdatableMember<Entity> parent)
    {
        return new EntityChildAccessor(name.ToString(), parent);
    }

    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name, UpdatableMember<Entity> parent)
    {
        // Note: we currently only support component with data contract aliases
        var dotIndex = name.LastIndexOf('.');

        // TODO: Temporary hack to get static field of the requested type/property name
        // Need to have access to DataContract name<=>type mapping in the runtime (only accessible in Stride.Core.Design now)
        var typeName = (dotIndex == -1) ? name : name[..dotIndex];
        var type = DataSerializerFactory.GetTypeFromAlias(typeName.ToString());
        if (type == null)
            throw new InvalidOperationException($"Can't find a type with alias {typeName}; did you properly set a DataContractAttribute with this alias?");

        return UpdateEngine.CreateEntityComponentAccessor(type, parent);
    }

    private sealed class EntityChildAccessor(string childName, UpdatableMember<Entity> parent) : UpdatableMember<Entity>
    {
        public override bool SupportsByReference => false;

        public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name)
        {
            return Instance.ResolveProperty(name, this);
        }
        public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name)
        {
            return Instance.ResolveIndexer(name, this);
        }

        public override Entity GetValue(object instance)
        {
            var entity = parent.GetValue(instance);
            if (entity != null)
            {
                foreach (var child in entity.Transform.Children)
                {
                    var childEntity = child.Entity;
                    if (childEntity.Name == childName)
                    {
                        return childEntity;
                    }
                }
            }
            return null;
        }

        public override ref Entity GetReference(object instance)
        {
            throw new NotSupportedException();
        }

        public override void SetValue(object instance, Entity value)
        {
            throw new NotSupportedException();
        }
    }
}
