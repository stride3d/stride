using System;
using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

internal sealed class BlittableArrayData<T>(int index, int offset, uint size) : UpdatableMember<T[]>
{
    public override string Name => $"Index_{index}_Offset_{offset}_Size_{size}";

    internal override Type MemberType => typeof(void);

    internal override bool IsBlittable => true;

    internal override bool IsIndexer => true;

    public int Index => index;

    public int Offset => offset;

    public uint Size => size;

    public override UpdatableMember GetOrCreateIndexer(string name)
    {
        throw new NotSupportedException();
    }

    public override UpdatableMember GetOrCreateProperty(string name)
    {
        throw new NotSupportedException();
    }

    public override unsafe void Update(T[] parent, byte* data, UpdateObjectData[] updateObjects)
    {
        int* conditionPtr = (int*)(data + DataOffset);
        if (*conditionPtr != 0 && Index < parent.Length)
        {
            ref byte destination = ref Unsafe.AddByteOffset(ref Unsafe.As<T, byte>(ref parent[Index]), offset);
            ref byte source = ref Unsafe.AsRef<byte>(data + DataOffset + sizeof(int));
            Unsafe.CopyBlock(ref destination, ref source, size);
        }
    }

    public override unsafe void Update(ref T[] parent, byte* data, UpdateObjectData[] updateObjects)
    {
        throw new NotSupportedException();
    }
}
