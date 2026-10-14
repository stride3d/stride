using System.Collections.Generic;

namespace Stride.Updater.New;

internal sealed class ListIndexAccessor<TList, TValue>(string name, int index, UpdatableType<TValue> elementType) : UpdatableMember<TList, TValue>, IUpdatableMember<ListIndexAccessor<TList, TValue>, TList, TValue>
    where TList : IList<TValue>
{
    public static bool SupportsByReference => false;

    public override string Name => name;

    internal int Index => index;

    internal override bool IsIndexer => true;

    public override UpdatableMember<TValue> CreateProperty(string name)
    {
        return elementType.CreateProperty(name);
    }

    public override UpdatableMember<TValue> CreateIndexer(string name)
    {
        return elementType.CreateIndexer(name);
    }

    public static TValue GetValue(ListIndexAccessor<TList, TValue> @this, TList parent)
    {
        if (parent.Count > @this.Index)
        {
            return parent[@this.Index];
        }
        else
        {
            return default;
        }
    }

    public static void SetValue(ListIndexAccessor<TList, TValue> @this, TList parent, TValue value)
    {
        if (parent.Count > @this.Index)
        {
            parent[@this.Index] = value;
        }
    }

    public override unsafe void Update(TList parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override unsafe void Update(ref TList parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);
}
