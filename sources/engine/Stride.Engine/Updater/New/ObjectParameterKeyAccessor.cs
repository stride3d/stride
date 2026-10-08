using Stride.Rendering;

namespace Stride.Updater.New;

internal sealed class ObjectParameterKeyAccessor<T>(string name, ObjectParameterKey<T> parameterKey, UpdatableType<T> type) : UpdatableMember<ParameterCollection, T> where T : class
{
    protected override bool SupportsByReference => false;

    public override string Name => name;

    internal override bool IsIndexer => true;

    protected override T GetValue(ParameterCollection parent)
    {
        return parent.Get(parameterKey);
    }

    protected override void SetValue(ParameterCollection parent, T value)
    {
        parent.Set(parameterKey, value);
    }

    public override UpdatableMember<T> CreateIndexer(string name)
    {
        return type.CreateIndexer(name);
    }

    public override UpdatableMember<T> CreateProperty(string name)
    {
        return type.CreateProperty(name);
    }
}
