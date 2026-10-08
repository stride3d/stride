using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

internal sealed class ArrayIndexAccessor<T>(string name, int index, UpdatableType<T> elementType) : UpdatableMember<T[], T>
{
    protected override bool SupportsByReference => true;

    public override string Name => name;

    internal override bool IsIndexer => true;

    public override UpdatableMember<T> CreateProperty(string name)
    {
        return elementType.CreateProperty(name);
    }

    public override UpdatableMember<T> CreateIndexer(string name)
    {
        return elementType.CreateIndexer(name);
    }

    protected override T GetValue(T[] parent)
    {
        if (parent.Length > index)
        {
            return parent[index];
        }
        else
        {
            return default;
        }
    }

    protected override ref T GetReference(T[] parent)
    {
        if (parent.Length > index)
        {
            return ref parent[index];
        }
        else
        {
            return ref Unsafe.NullRef<T>();
        }
    }

    protected override void SetValue(T[] parent, T value)
    {
        if (parent.Length > index)
        {
            parent[index] = value;
        }
    }
}
