using System;
using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

public abstract class UpdatableMember
{
    internal abstract void SetObject(object instance, object value);
    internal abstract unsafe void SetBlittable(object instance, byte* value);
    public abstract UpdatableMember ResolveProperty(ReadOnlySpan<char> name);
    public abstract UpdatableMember ResolveIndexer(ReadOnlySpan<char> name);
    public abstract bool IsBlittable { get; }
    public abstract bool SupportsByReference { get; }
    public abstract Type MemberType { get; }
}
public abstract class UpdatableMember<T> : UpdatableMember
{
    internal sealed override void SetObject(object instance, object value)
    {
        SetValue(instance, (T)value);
    }

    internal sealed override unsafe void SetBlittable(object instance, byte* value)
    {
        SetValue(instance, Unsafe.AsRef<T>(value));
    }

    public virtual T GetValue(object instance)
    {
        ref T reference = ref GetReference(instance);
        return Unsafe.IsNullRef(ref reference) ? default : reference;
    }

    public abstract ref T GetReference(object instance);

    public virtual void SetValue(object instance, T value)
    {
        ref T reference = ref GetReference(instance);
        if (!Unsafe.IsNullRef(ref reference))
        {
            reference = value;
        }
    }

    public sealed override bool IsBlittable => !RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    public sealed override Type MemberType => typeof(T);
}
