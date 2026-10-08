using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Stride.Updater.New;

public abstract class UpdatableMember
{
    public abstract UpdatableMember GetOrCreateProperty(string name);
    public abstract UpdatableMember GetOrCreateIndexer(string name);
    internal virtual UpdatableMember GetOrCreateCast<TCast>(string name, UpdatableType<TCast> type) where TCast : class
    {
        throw new NotSupportedException();
    }
    internal void MakeLeaf(int dataOffset)
    {
        DataOffset = dataOffset;
    }

    internal int DataOffset { get; private set; } = -1;
    internal bool IsLeaf => DataOffset >= 0;
    internal abstract bool IsBlittable { get; }
    internal virtual bool IsIndexer => false;
    internal virtual bool IsCast => false;
    internal abstract Type MemberType { get; }
    public abstract string Name { get; }
    public override string ToString()
    {
        return Name;
    }
    internal virtual void Optimize()
    {
    }
    internal virtual bool TryMergeWithParent<TGrandParent>(UpdatableMember<TGrandParent> parent, out UpdatableMember<TGrandParent> merged)
    {
        merged = null;
        return false;
    }
}
public abstract class UpdatableMember<TParent> : UpdatableMember
{
    public abstract unsafe void Update(TParent parent, byte* data, UpdateObjectData[] updateObjects);
    public abstract unsafe void Update(ref TParent parent, byte* data, UpdateObjectData[] updateObjects);
    internal virtual bool TryReduce(out UpdatableMember<TParent> reducedMember)
    {
        reducedMember = null;
        return false;
    }
    internal virtual UpdatableMemberRewrite<TParent> OptimizeAndRewrite()
    {
        Optimize();
        return new(this, []);
    }
}
public abstract class UpdatableMember<TParent, TThis> : UpdatableMember<TParent>
{
    protected List<UpdatableMember<TThis>> Children { get; } = [];
    internal sealed override Type MemberType => typeof(TThis);
    internal sealed override bool IsBlittable => !RuntimeHelpers.IsReferenceOrContainsReferences<TThis>();
    internal sealed override UpdatableMember GetOrCreateCast<TCast>(string name, UpdatableType<TCast> type) where TCast : class
    {
        foreach (var child in Children)
        {
            if (child.Name == name && child is UpdatableClassCast<TThis, TCast>)
            {
                return child;
            }
        }
        var newChild = new UpdatableClassCast<TThis, TCast>(name, type);
        Children.Add(newChild);
        return newChild;
    }
    protected static unsafe void Update<T>(T @this, TParent parent, byte* data, UpdateObjectData[] updateObjects) where T : UpdatableMember<TParent, TThis>, IUpdatableMember<T, TParent, TThis>
    {
        Debug.Assert(!typeof(TParent).IsValueType, "Value types should call the other Update overload.");
        if (@this.IsLeaf)
        {
            // Leaf node, update the value directly
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TThis>())
            {
                UpdateObjectData updateObject = updateObjects[@this.DataOffset];
                if (updateObject.Condition != 0)
                {
                    T.SetValue(@this, parent, (TThis)updateObject.Value);
                }
            }
            else
            {
                int* conditionPtr = (int*)(data + @this.DataOffset);
                if (*conditionPtr != 0)
                {
                    T.SetValue(@this, parent, Unsafe.AsRef<TThis>(data + @this.DataOffset + sizeof(int)));
                }
            }
        }
        else
        {
            // Not a leaf node, update the children
            if (!typeof(TThis).IsValueType)
            {
                TThis propertyValue = T.GetValue(@this, parent);
                if (propertyValue is null)
                {
                    return;
                }
                for (var i = 0; i < @this.Children.Count; i++)
                {
                    @this.Children[i].Update(propertyValue, data, updateObjects);
                }
            }
            else if (T.SupportsByReference)
            {
                ref TThis propertyValue = ref T.GetReference(@this, parent);
                if (Unsafe.IsNullRef(ref propertyValue))
                {
                    return;
                }
                for (var i = 0; i < @this.Children.Count; i++)
                {
                    @this.Children[i].Update(ref propertyValue, data, updateObjects);
                }
            }
            else
            {
                TThis propertyValue = T.GetValue(@this, parent);
                for (var i = 0; i < @this.Children.Count; i++)
                {
                    @this.Children[i].Update(ref propertyValue, data, updateObjects);
                }
                T.SetValue(@this, parent, propertyValue);
            }
        }
    }
    protected static unsafe void Update<T>(T @this, ref TParent parent, byte* data, UpdateObjectData[] updateObjects) where T : UpdatableMember<TParent, TThis>, IUpdatableMember<T, TParent, TThis>
    {
        Debug.Assert(typeof(TParent).IsValueType, "Reference types should call the other Update overload.");
        if (@this.IsLeaf)
        {
            // Leaf node, update the value directly
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TThis>())
            {
                UpdateObjectData updateObject = updateObjects[@this.DataOffset];
                if (updateObject.Condition != 0)
                {
                    T.SetValue(@this, ref parent, (TThis)updateObject.Value);
                }
            }
            else
            {
                int* conditionPtr = (int*)(data + @this.DataOffset);
                if (*conditionPtr != 0)
                {
                    T.SetValue(@this, ref parent, Unsafe.AsRef<TThis>(data + @this.DataOffset + sizeof(int)));
                }
            }
        }
        else
        {
            // Not a leaf node, update the children
            if (!typeof(TThis).IsValueType)
            {
                TThis propertyValue = T.GetValue(@this, ref parent);
                if (propertyValue is null)
                {
                    return;
                }
                for (var i = 0; i < @this.Children.Count; i++)
                {
                    @this.Children[i].Update(propertyValue, data, updateObjects);
                }
            }
            else if (T.SupportsByReference)
            {
                ref TThis propertyValue = ref T.GetReference(@this, ref parent);
                if (Unsafe.IsNullRef(ref propertyValue))
                {
                    return;
                }
                for (var i = 0; i < @this.Children.Count; i++)
                {
                    @this.Children[i].Update(ref propertyValue, data, updateObjects);
                }
            }
            else
            {
                TThis propertyValue = T.GetValue(@this, ref parent);
                for (var i = 0; i < @this.Children.Count; i++)
                {
                    @this.Children[i].Update(ref propertyValue, data, updateObjects);
                }
                T.SetValue(@this, ref parent, propertyValue);
            }
        }
    }
    public sealed override UpdatableMember<TThis> GetOrCreateProperty(string name)
    {
        foreach (var child in Children)
        {
            if (child.Name == name && !child.IsIndexer && !child.IsCast)
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
    internal override void Optimize()
    {
        for (var i = 0; i < Children.Count; i++)
        {
            var rewrite = Children[i].OptimizeAndRewrite();
            Children[i] = rewrite.Member;
            Children.AddRange(rewrite.PromotedMembers);
        }

        // Sorting improves the performance of data access
        Children.Sort(ChildComparer.Instance);
    }
    internal override UpdatableMemberRewrite<TParent> OptimizeAndRewrite()
    {
        Optimize();

        UpdatableMember<TParent> member = this;
        List<UpdatableMember<TParent>> promotedMembers = null;
        for (var i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].TryMergeWithParent(this, out var mergedChild))
            {
                var replaceMember = Children.Count == 1;
                Children.RemoveAt(i);
                if (replaceMember)
                {
                    member = mergedChild;
                    break;
                }

                promotedMembers ??= [];
                promotedMembers.Add(mergedChild);
            }
        }

        // Disabled because the Blittable* classes seem to be slower than the normal classes now that things have been more optimized with static virtual methods.
        //if (member.TryReduce(out var reducedMember))
        {
            //member = reducedMember;
        }

        return new(member, (IReadOnlyList<UpdatableMember<TParent>>)promotedMembers ?? []);
    }

    private sealed class ChildComparer : IComparer<UpdatableMember<TThis>>
    {
        public static readonly ChildComparer Instance = new();
        public int Compare(UpdatableMember<TThis> x, UpdatableMember<TThis> y)
        {
            // Leaf nodes should come before non-leaf nodes
            if (!x.IsLeaf)
            {
                return y.IsLeaf ? 1 : 0;
            }
            if (!y.IsLeaf)
            {
                return -1;
            }

            // Blittable nodes should come before non-blittable nodes
            if (!x.IsBlittable && y.IsBlittable)
            {
                return 1;
            }
            if (x.IsBlittable && !y.IsBlittable)
            {
                return -1;
            }

            // Both are leaf nodes with the same blittable status, compare by data offset
            return x.DataOffset.CompareTo(y.DataOffset);
        }
    }
}
