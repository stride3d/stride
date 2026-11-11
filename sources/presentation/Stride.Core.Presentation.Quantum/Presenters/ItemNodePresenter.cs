// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters;

public class ItemNodePresenter : NodePresenterBase
{
    private IObjectNode? parentingNode;
    private bool isDisposed;

    protected readonly IObjectNode Container;

    public ItemNodePresenter(INodePresenterFactoryInternal factory, IPropertyProviderViewModel? propertyProvider, INodePresenter parent, IObjectNode container, NodeIndex index)
        : base(factory, propertyProvider, parent)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(parent);
        Container = container ?? throw new ArgumentNullException(nameof(container));
        var innerCollectionType = container.Descriptor.GetInnerCollectionType();
        Descriptor = TypeDescriptorFactory.Default.Find(innerCollectionType);
        OwnerCollection = parent;
        Type = innerCollectionType;
        Index = index;
        Name = index.ToString();
        Order = index.IsInt ? (int?)index.Int : null; // So items are sorted by index instead of string
        CombineKey = Name;
        DisplayName = (container.Descriptor.Category != DescriptorCategory.Dictionary && Index.IsInt)
                    ? "Item " + Index
                    : Index.ToString();

        container.ItemChanging += OnItemChanging;
        container.ItemChanged += OnItemChanged;

        parentingNode = Container.ItemReferences != null ? Container.IndexedTarget(Index) : null;

        NestedItemSubscribe();
        AttachCommands();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            isDisposed = true;
            Container.ItemChanging -= OnItemChanging;
            Container.ItemChanged -= OnItemChanged;

            NestedItemUnsubscribe();
        }
    }

    public INodePresenter OwnerCollection { get; }

    public sealed override NodeIndex Index { get; }

    public override Type Type { get; }

    public override bool IsEnumerable => Container.IsEnumerable;

    public override ITypeDescriptor? Descriptor { get; }

    public override object Value
    {
        get
        {
            // The item can be removed from the container (e.g. a dictionary key) after the presenter was created:
            // in that case the index no longer resolves to a value, so report it as null instead of throwing.
            if (Container.ItemReferences is { } itemReferences && !itemReferences.HasIndex(Index))
                return null!;
            return Container.Retrieve(Index);
        }
    }

    protected override IObjectNode? ParentingNode => parentingNode;

    public override void UpdateValue(object newValue)
    {
        try
        {
            Container.Update(newValue, Index);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while updating the value of the node, see the inner exception for more information.", e);
        }
    }

    public override void AddItem(object value)
    {
        if (Container.IndexedTarget(Index)?.IsEnumerable != true)
            throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(AddItem)} cannot be invoked on members that are not collection.");

        try
        {
            Container.IndexedTarget(Index).Add(value);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
        }
    }

    public override void AddItem(object value, NodeIndex index)
    {
        if (Container.IndexedTarget(Index)?.IsEnumerable != true)
            throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(AddItem)} cannot be invoked on members that are not collection.");

        try
        {
            Container.IndexedTarget(Index).Add(value, index);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while adding an item to the node, see the inner exception for more information.", e);
        }
    }

    public override void RemoveItem(object value, NodeIndex index)
    {
        if (Container.IndexedTarget(Index)?.IsEnumerable != true)
            throw new NodePresenterException($"{nameof(MemberNodePresenter)}.{nameof(AddItem)} cannot be invoked on members that are not collection.");

        try
        {
            Container.IndexedTarget(Index).Remove(value, index);
        }
        catch (Exception e)
        {
            throw new NodePresenterException("An error occurred while removing an item to the node, see the inner exception for more information.", e);
        }
    }

    public override NodeAccessor GetNodeAccessor()
    {
        return new NodeAccessor(Container, Index);
    }

    private void OnItemChanging(object? sender, ItemChangeEventArgs e)
    {
        // The presenter can be disposed while the event is still being dispatched (its parent disposes its
        // children before the handlers of the other subscribers run): a disposed presenter must not react
        // to the remaining handlers, nor re-subscribe to a new item.
        if (isDisposed || !IsValidChange(e))
            return;
        RaiseValueChanging(e.NewValue);
    }

    private void OnItemChanged(object? sender, ItemChangeEventArgs e)
    {
        if (isDisposed || !IsValidChange(e))
        {
            return;
        }

        NestedItemUnsubscribe();

        parentingNode = Container.ItemReferences != null
            ? Container.IndexedTarget(Index)
            : null;

        NestedItemSubscribe();

        Refresh();
        RaiseValueChanged(e.OldValue);
    }

    private bool IsValidChange(ItemChangeEventArgs e)
    {
        return IsValidChange(e.ChangeType, e.Index, Index);
    }

    public static bool IsValidChange(ContentChangeType changeType, NodeIndex changeIndex, NodeIndex presenterIndex)
    {
        // We should care only if the change is an update at the same index.
        // Other scenarios (add, remove) are handled by the parent node.
        return changeType switch
        {
            ContentChangeType.CollectionUpdate => Equals(changeIndex, presenterIndex),
            ContentChangeType.CollectionAdd or ContentChangeType.CollectionRemove => false,
            _ => throw new ArgumentOutOfRangeException(nameof(changeType)),
        };
    }

    private void NestedItemSubscribe()
    {
        if (parentingNode != null)
        {
            parentingNode.ItemChanging += OnNestedItemChanging;
            parentingNode.ItemChanged += OnNestedItemChanged;
        }
    }

    private void NestedItemUnsubscribe()
    {
        if (parentingNode != null)
        {
            parentingNode.ItemChanging -= OnNestedItemChanging;
            parentingNode.ItemChanged -= OnNestedItemChanged;
        }
    }

    private void OnNestedItemChanging(object? sender, ItemChangeEventArgs e)
    {
        if (!isDisposed)
            RaiseValueChanging(Value);
    }

    private void OnNestedItemChanged(object? sender, ItemChangeEventArgs e)
    {
        if (isDisposed)
            return;
        Refresh();
        RaiseValueChanged(Value);
    }
}
