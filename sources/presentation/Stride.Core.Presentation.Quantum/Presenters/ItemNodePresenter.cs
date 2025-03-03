// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters;

public class ItemNodePresenter : NodePresenterBase
{
    protected readonly IObjectNode Container;

    public ItemNodePresenter(INodePresenterFactoryInternal factory, IPropertyProviderViewModel? propertyProvider, INodePresenter parent, IObjectNode container, NodeIndex index)
        : base(factory, propertyProvider, parent)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(parent);
        Container = container ?? throw new ArgumentNullException(nameof(container));
        Descriptor = TypeDescriptorFactory.Default.Find(container.Descriptor.GetInnerCollectionType());
        OwnerCollection = parent;
        if (container.Descriptor is CollectionDescriptor collectionDescriptor)
        {
            Type = collectionDescriptor.ElementType;
        }
        else if (container.Descriptor is DictionaryDescriptor dictionaryDescriptor)
        {
            Type = dictionaryDescriptor.ValueType;
        }
        else if (container.Descriptor is ArrayDescriptor arrayDescriptor)
        {
            Type = arrayDescriptor.ElementType;
        }
        Index = index;
        Name = index.ToString();
        Order = index.IsInt ? (int?)index.Int : null; // So items are sorted by index instead of string
        CombineKey = Name;
        DisplayName = (container.Descriptor.Category != DescriptorCategory.Dictionary && Index.IsInt)
                    ? "Item " + Index
                    : Index.ToString();

        container.ItemChanging += OnItemChanging;
        container.ItemChanged += OnItemChanged;
        AttachCommands();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Container.ItemChanging -= OnItemChanging;
            Container.ItemChanged -= OnItemChanged;
        }
    }

    public INodePresenter OwnerCollection { get; }

    public sealed override NodeIndex Index { get; }

    public override Type Type { get; }

    public override bool IsEnumerable => Container.IsEnumerable;

    public override ITypeDescriptor? Descriptor { get; }

    public override object Value => Container.Retrieve(Index);

    protected override IObjectNode? ParentingNode => Container.ItemReferences != null ? Container.IndexedTarget(Index) : null;

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
        if (IsValidChange(e))
            RaiseValueChanging(e.NewValue);
    }

    private void OnItemChanged(object? sender, ItemChangeEventArgs e)
    {
        if (IsValidChange(e))
        {
            Refresh();
            RaiseValueChanged(e.OldValue);
        }
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
}
