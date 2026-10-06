using System;
using Stride.Core;
using Stride.Rendering;

namespace Stride.Updater.New;

public sealed class ParameterCollectionType : UpdatableType<ParameterCollection>
{
    public static ParameterCollectionType Instance { get; } = new();

    [ModuleInitializer]
    internal static void Register()
    {
        UpdateEngine.RegisterType(typeof(ParameterCollection), Instance);
    }

    public override UpdatableMember ResolveProperty(ReadOnlySpan<char> name, UpdatableMember<ParameterCollection> parent)
    {
        throw new NotSupportedException("ParameterCollection does not have properties.");
    }

    public override UpdatableMember ResolveIndexer(ReadOnlySpan<char> name, UpdatableMember<ParameterCollection> parent)
    {
        var key = ParameterKeys.FindByName(name.ToString()) ?? throw new InvalidOperationException($"Property Key path parse error: could not parse indexer value '{name}'");

        return UpdateEngine.CreateParameterKeyAccessor(key, parent);
    }
}
