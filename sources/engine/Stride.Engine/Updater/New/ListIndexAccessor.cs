using System.Collections.Generic;

namespace Stride.Updater.New;

internal sealed class ListIndexAccessor<TList, TValue>(string name, int index, UpdatableType<TValue> elementType) : UpdatableMember<TList, TValue>
    where TList : IList<TValue>
{
    protected override bool SupportsByReference => false;

    public override string Name => name;

    public override UpdatableMember<TValue> CreateProperty(string name)
    {
        return elementType.CreateProperty(name);
    }

    public override UpdatableMember<TValue> CreateIndexer(string name)
    {
        return elementType.CreateIndexer(name);
    }

    protected override TValue GetValue(TList parent)
    {
        if (parent.Count > index)
        {
            return parent[index];
        }
        else
        {
            return default;
        }
    }

    protected override void SetValue(TList parent, TValue value)
    {
        if (parent.Count > index)
        {
            parent[index] = value;
        }
    }
}
