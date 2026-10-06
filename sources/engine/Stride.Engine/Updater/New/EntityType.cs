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

    public override UpdatableMember<Entity> CreateProperty(string name)
    {
        return new EntityChildAccessor(name);
    }

    public override UpdatableMember<Entity> CreateIndexer(string name)
    {
        // Note: we currently only support component with data contract aliases
        var dotIndex = name.LastIndexOf('.');

        // TODO: Temporary hack to get static field of the requested type/property name
        // Need to have access to DataContract name<=>type mapping in the runtime (only accessible in Stride.Core.Design now)
        var typeName = (dotIndex == -1) ? name : name[..dotIndex];
        var type = DataSerializerFactory.GetTypeFromAlias(typeName);
        if (type == null)
            throw new InvalidOperationException($"Can't find a type with alias {typeName}; did you properly set a DataContractAttribute with this alias?");

        return UpdateEngine.CreateEntityComponentAccessor(type);
    }
}
