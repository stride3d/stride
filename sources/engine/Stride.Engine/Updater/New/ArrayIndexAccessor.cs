using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

internal sealed class ArrayIndexAccessor<T>(string name, int index, UpdatableType<T> elementType) : UpdatableMember<T[], T>, IUpdatableMember<ArrayIndexAccessor<T>, T[], T>
{
    public static bool SupportsByReference => true;

    internal override bool IsIndexer => true;

    public override string Name => name;

    public int Index => index;

    public override UpdatableMember<T> CreateProperty(string name)
    {
        return elementType.CreateProperty(name);
    }

    public override UpdatableMember<T> CreateIndexer(string name)
    {
        return elementType.CreateIndexer(name);
    }

    public static T GetValue(ArrayIndexAccessor<T> @this, T[] parent)
    {
        if (parent.Length > @this.Index)
        {
            return parent[@this.Index];
        }
        else
        {
            return default;
        }
    }

    public static ref T GetReference(ArrayIndexAccessor<T> @this, T[] parent)
    {
        if (parent.Length > @this.Index)
        {
            return ref parent[@this.Index];
        }
        else
        {
            return ref Unsafe.NullRef<T>();
        }
    }

    public static void SetValue(ArrayIndexAccessor<T> @this, T[] parent, T value)
    {
        if (parent.Length > @this.Index)
        {
            parent[@this.Index] = value;
        }
    }

    public override unsafe void Update(T[] parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override unsafe void Update(ref T[] parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);

    internal override bool TryReduce(out UpdatableMember<T[]> reducedMember)
    {
        if (IsBlittable && IsLeaf)
        {
            reducedMember = new BlittableArrayData<T>(index, 0, (uint)Unsafe.SizeOf<T>());
            return true;
        }
        return base.TryReduce(out reducedMember);
    }
}
