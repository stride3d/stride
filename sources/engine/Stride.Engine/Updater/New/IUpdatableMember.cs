using System;

namespace Stride.Updater.New;

public interface IUpdatableMember<TSelf, TParent, TThis> where TSelf : UpdatableMember<TParent, TThis>
{
    static abstract bool SupportsByReference { get; }
    static virtual TThis GetValue(TSelf @this, TParent parent) => throw new NotSupportedException();
    static virtual TThis GetValue(TSelf @this, ref TParent parent) => throw new NotSupportedException();
    static virtual ref TThis GetReference(TSelf @this, TParent parent) => throw new NotSupportedException();
    static virtual ref TThis GetReference(TSelf @this, ref TParent parent) => throw new NotSupportedException();
    static virtual void SetValue(TSelf @this, TParent parent, TThis value) => throw new NotSupportedException();
    static virtual void SetValue(TSelf @this, ref TParent parent, TThis value) => throw new NotSupportedException();
}
