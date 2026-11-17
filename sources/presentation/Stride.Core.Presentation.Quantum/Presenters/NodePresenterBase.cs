// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters;

public abstract class NodePresenterBase : IInitializingNodePresenter
{
    private readonly INodePresenterFactoryInternal factory;
    private readonly List<INodePresenter> children = [];
    private HashSet<INodePresenter>? dependencies;
    // The dependencies added by AddDependency: any change of theirs refreshes this node
    private HashSet<INodePresenter>? refreshingDependencies;

    protected NodePresenterBase(INodePresenterFactoryInternal factory, IPropertyProviderViewModel? propertyProvider, INodePresenter? parent)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Parent = parent;
        PropertyProvider = propertyProvider;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (dependencies != null)
            {
                foreach (var dependency in dependencies)
                {
                    dependency.ValueChanged -= DependencyChanged;
                }
            }
        }
    }

    public INodePresenter this[string childName] => TryGetChild(childName) ?? throw new KeyNotFoundException($"Key {childName} not found in {nameof(INodePresenter)}");

    public INodePresenter Root => Parent?.Root ?? this;

    public INodePresenter? Parent { get; private set; }

    public IReadOnlyList<INodePresenter> Children => children;

    public string DisplayName { get; set; } = null!;

    public string Name { get; protected set; } = null!;

    public List<INodePresenterCommand> Commands { get; } = [];

    public abstract Type Type { get; }

    public abstract bool IsEnumerable { get; }

    public bool IsVisible { get; set; } = true;

    public bool IsReadOnly { get; set; }

    public bool RefreshOnValueChange { get; set; }

    public int? Order { get; set; }

    public abstract NodeIndex Index { get; }

    public abstract ITypeDescriptor? Descriptor { get; }

    public abstract object? Value { get; }

    public string CombineKey { get; set; } = null!;

    public PropertyContainerClass AttachedProperties { get; } = [];

    public event EventHandler<ValueChangingEventArgs>? ValueChanging;

    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    protected abstract IObjectNode? ParentingNode { get; }

    public abstract void UpdateValue(object? newValue);

    public abstract void AddItem(object value);

    public abstract void AddItem(object value, NodeIndex index);

    public abstract void RemoveItem(object value, NodeIndex index);

    public abstract NodeAccessor GetNodeAccessor();

    public IPropertyProviderViewModel? PropertyProvider { get; }

    public INodePresenterFactory Factory => factory;

    public override string ToString()
    {
        return $"[{GetType().Name}] {Name} (Count = {Children.Count}";
    }

    public void ChangeParent(INodePresenter newParent)
    {
        ArgumentNullException.ThrowIfNull(newParent);

        var parent = (NodePresenterBase?)Parent;
        parent?.children.Remove(this);

        parent = (NodePresenterBase)newParent;
        parent.children.Add(this);

        Parent = newParent;
    }

    public void Rename(string newName, bool overwriteCombineKey = true)
    {
        Name = newName;
        if (overwriteCombineKey)
        {
            CombineKey = newName;
        }
    }

    public INodePresenter? TryGetChild(string childName)
    {
        return children.FirstOrDefault(x => string.Equals(x.Name, childName, StringComparison.Ordinal));
    }

    public void AddDependency(INodePresenter node, bool refreshOnNestedNodeChanges)
    {
        ArgumentNullException.ThrowIfNull(node);

        Subscribe(node);
        (refreshingDependencies ??= []).Add(node);
    }

    public void AddValueDependency(INodePresenter node)
    {
        ArgumentNullException.ThrowIfNull(node);

        Subscribe(node);
    }

    protected void Refresh()
    {
        // Remove existing children and attached properties
        foreach (var child in children.DepthFirst(x => x.Children))
        {
            child.Dispose();
        }
        children.Clear();
        AttachedProperties.Clear();

        // And recompute them from the current value.
        factory.CreateChildren(this, ParentingNode, PropertyProvider);
    }

    /// <summary>
    /// Gets whether a change from <paramref name="oldValue"/> to <paramref name="newValue"/> changes the value only, so that this
    /// presenter keeps its children and is not refreshed.
    /// </summary>
    /// <param name="oldValue">The value before the change.</param>
    /// <param name="newValue">The value after the change.</param>
    /// <param name="hasObjectNode">Whether the value has an object node of its own (its members or its items are shown as children).</param>
    /// <param name="declaredType">The declared type of the value, or <c>null</c> if unknown.</param>
    /// <returns><c>true</c> if both values are leaves of the same type and nothing asks for a refresh, <c>false</c> otherwise.</returns>
    /// <remarks>
    /// A value written to a member of a value type is converted to that type (an <see cref="int"/> written to a <see cref="float"/>,
    /// for example, as an undo of a write made through a row of another type does): the values of such a member always have the same type.
    /// </remarks>
    /// <seealso cref="INodePresenter.RefreshOnValueChange"/>
    protected bool IsValueOnlyChange(object? oldValue, object? newValue, bool hasObjectNode, Type? declaredType)
    {
        return !RefreshOnValueChange && !hasObjectNode && oldValue is not null && newValue is not null
               && (oldValue.GetType() == newValue.GetType() || declaredType is { IsValueType: true });
    }

    /// <summary>
    /// Gets whether a change of an item of <paramref name="collection"/> changes the value of that item only, so that the
    /// presenter of the collection keeps its children and is not refreshed.
    /// </summary>
    /// <param name="collection">The node of the collection.</param>
    /// <param name="e">The change of the item.</param>
    /// <returns><c>true</c> if an item was replaced by a leaf of the same type, <c>false</c> otherwise (an item added or removed, for example).</returns>
    protected bool IsValueOnlyItemChange(IObjectNode? collection, ItemChangeEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return e.ChangeType == ContentChangeType.CollectionUpdate && collection is not null && IsValueOnlyChange(e.OldValue, e.NewValue, HasItemNode(collection, e.Index), null);
    }

    /// <summary>
    /// Gets whether the item at <paramref name="index"/> of <paramref name="collection"/> has an object node of its own.
    /// </summary>
    protected static bool HasItemNode(IObjectNode collection, NodeIndex index)
    {
        ArgumentNullException.ThrowIfNull(collection);
        return collection.ItemReferences is { } references && references.HasIndex(index) && collection.IndexedTarget(index) is not null;
    }

    protected void AttachCommands()
    {
        foreach (var command in factory.AvailableCommands)
        {
            if (command.CanAttach(this))
                Commands.Add(command);
        }
    }

    protected void RaiseValueChanging(object? newValue)
    {
        ValueChanging?.Invoke(this, new ValueChangingEventArgs(newValue));
    }

    protected void RaiseValueChanged(object? oldValue)
    {
        RaiseValueChanged(oldValue, false);
    }

    protected void RaiseValueChanged(object? oldValue, bool isValueOnly)
    {
        ValueChanged?.Invoke(this, new ValueChangedEventArgs(oldValue, isValueOnly));
    }

    /// <summary>
    /// Raises <see cref="ValueChanging"/> for a change of the value only, on this presenter and on its descendants (the virtual
    /// nodes that the updaters add under a leaf, such as the components of a vector, show a part of its value).
    /// </summary>
    /// <param name="newValue">The new value of the node.</param>
    protected void RaiseValueOnlyChanging(object? newValue)
    {
        RaiseValueChanging(newValue);
        // A handler can refresh a node of the tree: take the descendants before raising the events
        foreach (var descendant in children.DepthFirst(x => x.Children).ToList())
        {
            (descendant as NodePresenterBase)?.RaiseValueChanging(descendant.Value);
        }
    }

    /// <summary>
    /// Raises <see cref="ValueChanged"/> for a change of the value only, on this presenter and on its descendants.
    /// </summary>
    /// <param name="oldValue">The old value of the node.</param>
    /// <seealso cref="RaiseValueOnlyChanging"/>
    protected void RaiseValueOnlyChanged(object? oldValue)
    {
        RaiseValueChanged(oldValue, true);
        foreach (var descendant in children.DepthFirst(x => x.Children).ToList())
        {
            (descendant as NodePresenterBase)?.RaiseValueChanged(descendant.Value, true);
        }
    }

    private void Subscribe(INodePresenter node)
    {
        dependencies ??= [];
        if (dependencies.Add(node))
        {
            node.ValueChanged += DependencyChanged;
        }
    }

    private void DependencyChanged(object? sender, ValueChangedEventArgs e)
    {
        if (e.IsValueOnly && sender is INodePresenter dependency && refreshingDependencies?.Contains(dependency) != true)
        {
            // A dependency added by AddValueDependency: this node shows its value, and only that changed
            RaiseValueChanging(Value);
            RaiseValueChanged(Value, true);
            return;
        }

        RaiseValueChanging(Value);
        Refresh();
        RaiseValueChanged(Value);
    }

    void IInitializingNodePresenter.AddChild(IInitializingNodePresenter child)
    {
        children.Add(child);
    }

    void IInitializingNodePresenter.FinalizeInitialization()
    {
    }
}
