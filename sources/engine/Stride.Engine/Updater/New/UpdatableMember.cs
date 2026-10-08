using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

public abstract class UpdatableMember
{
    public abstract UpdatableMember GetOrCreateProperty(string name);
    public abstract UpdatableMember GetOrCreateIndexer(string name);
    internal void MakeLeaf(int dataOffset)
    {
        DataOffset = dataOffset;
    }

    internal int DataOffset { get; private set; } = -1;
    internal bool IsLeaf => DataOffset >= 0;
    internal virtual bool IsIndexer => false;
    internal abstract Type MemberType { get; }
    public abstract string Name { get; }
    public override string ToString()
    {
        return Name;
    }
}
public abstract class UpdatableMember<TParent> : UpdatableMember
{
    public abstract unsafe void Update(TParent parent, byte* data, UpdateObjectData[] updateObjects);
    public abstract unsafe void Update(ref TParent parent, byte* data, UpdateObjectData[] updateObjects);
}
public abstract class UpdatableMember<TParent, TThis> : UpdatableMember<TParent>
{
    protected List<UpdatableMember<TThis>> Children { get; } = [];
    internal sealed override Type MemberType => typeof(TThis);
    public override unsafe void Update(TParent parent, byte* data, UpdateObjectData[] updateObjects)
    {
        Debug.Assert(!typeof(TParent).IsValueType, "Value types should call the other Update overload.");
        if (IsLeaf)
        {
            // Leaf node, update the value directly
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TThis>())
            {
                UpdateObjectData updateObject = updateObjects[DataOffset];
                if (updateObject.Condition != 0)
                {
                    SetValue(parent, (TThis)updateObject.Value);
                }
            }
            else
            {
                int* conditionPtr = (int*)(data + DataOffset);
                if (*conditionPtr != 0)
                {
                    SetValue(parent, Unsafe.AsRef<TThis>(data + DataOffset + sizeof(int)));
                }
            }
        }
        else
        {
            // Not a leaf node, update the children
            if (!typeof(TThis).IsValueType)
            {
                TThis propertyValue = GetValue(parent);
                if (propertyValue is null)
                {
                    return;
                }
                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Update(propertyValue, data, updateObjects);
                }
            }
            else if (SupportsByReference)
            {
                ref TThis propertyValue = ref GetReference(parent);
                if (Unsafe.IsNullRef(ref propertyValue))
                {
                    return;
                }
                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Update(ref propertyValue, data, updateObjects);
                }
            }
            else
            {
                TThis propertyValue = GetValue(parent);
                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Update(ref propertyValue, data, updateObjects);
                }
                SetValue(parent, propertyValue);
            }
        }
    }
    public override unsafe void Update(ref TParent parent, byte* data, UpdateObjectData[] updateObjects)
    {
        Debug.Assert(typeof(TParent).IsValueType, "Reference types should call the other Update overload.");
        if (IsLeaf)
        {
            // Leaf node, update the value directly
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TThis>())
            {
                UpdateObjectData updateObject = updateObjects[DataOffset];
                if (updateObject.Condition != 0)
                {
                    SetValue(ref parent, (TThis)updateObject.Value);
                }
            }
            else
            {
                int* conditionPtr = (int*)(data + DataOffset);
                if (*conditionPtr != 0)
                {
                    SetValue(ref parent, Unsafe.AsRef<TThis>(data + DataOffset + sizeof(int)));
                }
            }
        }
        else
        {
            // Not a leaf node, update the children
            if (!typeof(TThis).IsValueType)
            {
                TThis propertyValue = GetValue(ref parent);
                if (propertyValue is null)
                {
                    return;
                }
                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Update(propertyValue, data, updateObjects);
                }
            }
            else if (SupportsByReference)
            {
                ref TThis propertyValue = ref GetReference(ref parent);
                if (Unsafe.IsNullRef(ref propertyValue))
                {
                    return;
                }
                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Update(ref propertyValue, data, updateObjects);
                }
            }
            else
            {
                TThis propertyValue = GetValue(ref parent);
                for (var i = 0; i < Children.Count; i++)
                {
                    Children[i].Update(ref propertyValue, data, updateObjects);
                }
                SetValue(ref parent, propertyValue);
            }
        }
    }
    protected abstract bool SupportsByReference { get; }
    protected virtual TThis GetValue(TParent parent) => throw new NotSupportedException();
    protected virtual TThis GetValue(ref TParent parent) => throw new NotSupportedException();
    protected virtual ref TThis GetReference(TParent parent) => throw new NotSupportedException();
    protected virtual ref TThis GetReference(ref TParent parent) => throw new NotSupportedException();
    protected virtual void SetValue(TParent parent, TThis value) => throw new NotSupportedException();
    protected virtual void SetValue(ref TParent parent, TThis value) => throw new NotSupportedException();
    public sealed override UpdatableMember<TThis> GetOrCreateProperty(string name)
    {
        foreach (var child in Children)
        {
            if (child.Name == name && !child.IsIndexer)
            {
                return child;
            }
        }
        var newChild = CreateProperty(name);
        Children.Add(newChild);
        return newChild;
    }
    public override UpdatableMember<TThis> GetOrCreateIndexer(string name)
    {
        foreach (var child in Children)
        {
            if (child.Name == name && child.IsIndexer)
            {
                return child;
            }
        }
        var newChild = CreateIndexer(name);
        Children.Add(newChild);
        return newChild;
    }
    public abstract UpdatableMember<TThis> CreateProperty(string name);
    public abstract UpdatableMember<TThis> CreateIndexer(string name);
}
