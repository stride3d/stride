using System;
using Stride.Rendering;

namespace Stride.Updater.New;

internal sealed class ValueParameterKeyAccessor<T>(ValueParameterKey<T> parameterKey, UpdatableType<T> type) : UpdatableMember<ParameterCollection, T> where T : struct
{
    protected override bool SupportsByReference => false;

    public override string Name => parameterKey.Name;

    internal override bool IsIndexer => true;

    public override UpdatableMember<T> CreateProperty(string name)
    {
        return type.CreateProperty(name);
    }

    public override UpdatableMember<T> CreateIndexer(string name)
    {
        return type.CreateIndexer(name);
    }

    protected override T GetValue(ParameterCollection parent)
    {
        return parent.Get(parameterKey);
    }

    protected override void SetValue(ParameterCollection parent, T value)
    {
        parent.Set(parameterKey, value);
    }
}
