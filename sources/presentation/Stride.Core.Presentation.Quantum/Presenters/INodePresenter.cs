// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters;

public interface INodePresenter : IDisposable
{
    INodePresenter this[string childName] { get; }

    string DisplayName { get; set; }

    string Name { get; }

    INodePresenter Root { get; }

    INodePresenter? Parent { get; }

    IReadOnlyList<INodePresenter> Children { get; }

    List<INodePresenterCommand> Commands { get; }

    PropertyContainerClass AttachedProperties { get; }

    Type Type { get; }

    bool IsEnumerable { get; }

    bool IsReadOnly { get; set; }

    bool IsVisible { get; set; }

    /// <summary>
    /// Gets or sets whether a change of the value of this node always refreshes it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A node whose value is a leaf (a primitive, an enum, a string, a vector, a color, a content reference: a value with no
    /// object node of its own) and whose new value has the same type as the old one has only its value changed: it keeps its
    /// children and its attached properties, the updaters don't run again and the tree is not finalized, and its
    /// <see cref="ValueChanged"/> event says so (<see cref="ValueChangedEventArgs.IsValueOnly"/>). Any other change refreshes
    /// the node: its children are built again, the updaters run on it and the tree is finalized.
    /// </para>
    /// <para>
    /// An updater that reads the value of a leaf to change the tree (to show or hide other members in
    /// <see cref="INodePresenterUpdater.FinalizeTree"/>, or to set the attached properties of the node itself in
    /// <see cref="INodePresenterUpdater.UpdateNode"/>) sets this property on that node, so that its changes refresh it as before.
    /// An updater that changes another node from that value can add a dependency instead (see <see cref="AddDependency"/>).
    /// </para>
    /// </remarks>
    bool RefreshOnValueChange { get; set; }

    NodeIndex Index { get; }

    ITypeDescriptor? Descriptor { get; }

    int? Order { get; set; }

    object? Value { get; }

    string CombineKey { get; set; }

    IPropertyProviderViewModel? PropertyProvider { get; }

    INodePresenterFactory Factory { get; }

    event EventHandler<ValueChangingEventArgs>? ValueChanging;

    event EventHandler<ValueChangedEventArgs>? ValueChanged;

    void UpdateValue(object? newValue);

    void AddItem(object value);

    void AddItem(object value, NodeIndex index);

    void RemoveItem(object value, NodeIndex index);

    // TODO: this should probably be removed, UpdateValue should be called on the corresponding child node presenter itself

    NodeAccessor GetNodeAccessor();

    /// <summary>
    /// Adds a dependency to the given node.
    /// </summary>
    /// <param name="node">The node that should be a dependency of this node.</param>
    /// <param name="refreshOnNestedNodeChanges">If true, this node will also be refreshed when one of the child node of the dependency node changes.</param>
    /// <remarks>A node that is a dependency to this node will trigger a refresh of this node each time its value is modified (or the value of one of its parent).</remarks>
    void AddDependency(INodePresenter node, bool refreshOnNestedNodeChanges);

    /// <summary>
    /// Adds a dependency to the given node, whose value this node shows (an inlined member, for example).
    /// </summary>
    /// <param name="node">The node that should be a dependency of this node.</param>
    /// <remarks>
    /// When only the value of the dependency changes (see <see cref="ValueChangedEventArgs.IsValueOnly"/>), this node raises a
    /// change of its value only, without being refreshed. Any other change of the dependency refreshes this node, as a
    /// dependency added with <see cref="AddDependency"/> does.
    /// </remarks>
    void AddValueDependency(INodePresenter node);

    void ChangeParent(INodePresenter newParent);

    void Rename(string newName, bool overwriteCombineKey = true);

    INodePresenter? TryGetChild(string childName);
}
