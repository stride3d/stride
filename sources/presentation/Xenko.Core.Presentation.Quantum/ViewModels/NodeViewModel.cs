// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Windows;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.Reflection;
using Xenko.Core.TypeConverters;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Core;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Quantum;
using Expression = System.Linq.Expressions.Expression;

namespace Xenko.Core.Presentation.Quantum.ViewModels
{
    public class NodeViewModel : DispatcherViewModel, IDynamicMetaObjectProvider
    {
        internal class DifferentValuesObject { public readonly string Name = "DifferentValues"; };

        protected static readonly HashSet<string> ReservedNames = new HashSet<string>();
        private readonly AutoUpdatingSortedObservableCollection<NodeViewModel> children = new AutoUpdatingSortedObservableCollection<NodeViewModel>(new AnonymousComparer<NodeViewModel>(CompareChildren), nameof(Name), nameof(NodeIndex), nameof(Order));
        private readonly ObservableCollection<NodePresenterCommandWrapper> commands = new ObservableCollection<NodePresenterCommandWrapper>();
        private readonly Dictionary<string, object> associatedData = new Dictionary<string, object>();
        private readonly List<string> changingProperties = new List<string>();
        private readonly GraphViewModel owner;
        private readonly List<INodePresenter> nodePresenters;
        private List<NodeViewModel> initializingChildren = new List<NodeViewModel>();
        private bool isVisible;
        private bool isReadOnly;
        private string displayName;
        private bool isHighlighted;
        private bool valueChanging;

#if DEBUG
        private static bool DebugQuantumPropertyChanges = true;
#else
        private static bool DebugQuantumPropertyChanges = false;
#endif

        public static readonly object DifferentValues = new DifferentValuesObject();

        static NodeViewModel()
        {
            typeof(NodeViewModel).GetProperties().Select(x => x.Name).ForEach(x => ReservedNames.Add(x));
        }

        protected internal NodeViewModel([NotNull] GraphViewModel ownerViewModel, NodeViewModel parent, [NotNull] string baseName, Type nodeType, [NotNull] List<INodePresenter> nodePresenters)
            : base(ownerViewModel.ServiceProvider)
        {
            owner = ownerViewModel;
            Type = nodeType;

            if (baseName == null)
                throw new ArgumentException("baseName and index can't be both null.");

            Name = EscapeName(baseName);

            this.nodePresenters = nodePresenters;
            foreach (var nodePresenter in nodePresenters)
            {
                nodePresenter.ValueChanging += ValueChanging;
                nodePresenter.ValueChanged += ValueChanged;
                nodePresenter.AttachedProperties.PropertyUpdated += AttachedPropertyUpdated;
            }

            UpdateViewModelProperties();

            parent?.AddChild(this);
        }

        public override void Destroy()
        {
            foreach (var nodePresenter in nodePresenters)
            {
                nodePresenter.ValueChanging -= ValueChanging;
                nodePresenter.ValueChanged -= ValueChanged;
                nodePresenter.AttachedProperties.PropertyUpdated -= AttachedPropertyUpdated;
                nodePresenter.Dispose();
            }
            base.Destroy();
        }

        /// <summary>
        /// Gets or sets the name of this node. Note that the name can be used to access this node from its parent using a dynamic object.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the name used to display the node to the user.
        /// </summary>
        public string DisplayName { get => displayName; set => SetValue(ref displayName, value); }

        /// <summary>
        /// Gets the display path of this node. The path is constructed from the <see cref="DisplayName"/> of all nodes from the root to this one, separated by periods.
        /// </summary>
        public string DisplayPath { get { if (Parent == null) return string.Empty; var parentPath = Parent.DisplayPath; return parentPath != string.Empty ? parentPath + '.' + DisplayName : DisplayName; } }

        /// <summary>
        /// Gets or sets the value of the nodes represented by this view model.
        /// </summary>
        public object NodeValue { get => GetNodeValue(); set => SetNodeValue(ConvertValue(value)); }

        /// <summary>
        /// Gets the expected type of <see cref="NodeValue"/>.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the parent of this node.
        /// </summary>
        public NodeViewModel Parent { get; private set; }

        /// <summary>
        /// Gets the root of this node.
        /// </summary>
        [NotNull]
        public NodeViewModel Root {
            get
            {
                var root = this;
                while (root.Parent != null)
                    root = root.Parent;
                return root;
            }
        }

        /// <summary>
        /// Gets whether this node should be displayed in the view.
        /// </summary>
        public bool IsVisible { get => isVisible; private set => SetValue(ref isVisible, value); }

        /// <summary>
        /// Gets whether this node can be modified in the view.
        /// </summary>
        public bool IsReadOnly { get => isReadOnly; private set => SetValue(ref isReadOnly, value); }

        /// <summary>
        /// Gets the list of children nodes.
        /// </summary>
        public IReadOnlyCollection<NodeViewModel> Children => initializingChildren != null ? (IReadOnlyCollection<NodeViewModel>)initializingChildren : children;

        /// <summary>
        /// Gets the list of commands available in this node.
        /// </summary>
        public IEnumerable<NodePresenterCommandWrapper> Commands => commands;

        /// <summary>
        /// Gets additional data associated to this content. This can be used when the content itself does not contain enough information to be used as a view model.
        /// </summary>
        public IReadOnlyDictionary<string, object> AssociatedData => associatedData;

        /// <summary>
        /// Gets the level of depth of this node, starting from 0 for the root node.
        /// </summary>
        public int Level => Parent?.Level + 1 ?? 0;

        /// <summary>
        /// Gets the member info (if any).
        /// </summary>
        [CanBeNull]
        [Obsolete]
        public MemberInfo MemberInfo => null;

        /// <summary>
        /// Gets whether this node contains a collection.
        /// </summary>
        /// <remarks>Used mostly for sorting purpose.</remarks>
        /// <seealso cref="HasDictionary"/>
        public bool HasCollection => CollectionDescriptor.IsCollection(Type);

        /// <summary>
        /// Gets whether this node contains a dictionary.
        /// </summary>
        /// <remarks>Usually a dictionary is also a collection.</remarks>
        /// <seealso cref="HasCollection"/>
        public bool HasDictionary => DictionaryDescriptor.IsDictionary(Type);

        /// <summary>
        /// Gets the number of visible children.
        /// </summary>
        public int VisibleChildrenCount => Children.Count(x => x.IsVisible);

        // TODO: generalize usage in the templates
        public bool IsHighlighted { get => isHighlighted; set => SetValue(ref isHighlighted, value); }

        public IReadOnlyCollection<INodePresenter> NodePresenters => nodePresenters;

        /// <summary>
        /// Gets the path of this node. The path is constructed from the name of all nodes from the root to this one, separated by periods.
        /// </summary>
        protected string Path => Parent != null ? Parent.Path + '.' + Name : Name;

        /// <summary>
        /// Gets the order number of this node in its parent.
        /// </summary>
        protected int? Order => NodePresenters.First().Order;

        public void FinishInitialization()
        {
            if (initializingChildren != null)
            {
                OnPropertyChanging(nameof(Children));
                foreach (var child in initializingChildren)
                {
                    children.Add(child);
                }
                initializingChildren = null;
                OnPropertyChanged(nameof(Children));
            }

            var commonCommands = new Dictionary<INodePresenterCommand, int>();
            foreach (var nodePresenter in nodePresenters)
            {
                foreach (var command in nodePresenter.Commands)
                {
                    int count;
                    if (!commonCommands.TryGetValue(command, out count))
                    {
                        commonCommands.Add(command, 1);
                    }
                    else
                    {
                        commonCommands[command] = count + 1;
                    }
                }
            }
            foreach (var command in commonCommands)
            {
                if (command.Key.CombineMode == CombineMode.DoNotCombine && command.Value > 1)
                    continue;

                if (command.Key.CombineMode == CombineMode.CombineOnlyForAll && command.Value < nodePresenters.Count)
                    continue;

                var commandWrapper = ConstructCommandWrapper(command.Key);
                AddCommand(commandWrapper);
            }

            var commonAttachedProperties = nodePresenters.SelectMany(x => x.AttachedProperties).GroupBy(x => x.Key).ToList();
            foreach (var attachedProperty in commonAttachedProperties)
            {
                var combiner = attachedProperty.Key.Metadatas.OfType<PropertyCombinerMetadata>().FirstOrDefault()?.Combiner ?? DefaultCombineAttachedProperty;
                var values = attachedProperty.Select(x => x.Value).ToList();
                var value = values.Count == 1 ? values[0] : combiner(values);
                AddAssociatedData(attachedProperty.Key.Name, value);
            }
        }

        /// <summary>
        /// Returns the child node with the matching name.
        /// </summary>
        /// <param name="name">The name of the <see cref="Xenko.Core.Presentation.Quantum.ViewModels.NodeViewModel"/> to look for.</param>
        /// <returns>The corresponding child node, or <c>null</c> if no child with the given name exists.</returns>
        [CanBeNull]
        public NodeViewModel GetChild(string name)
        {
            name = EscapeName(name);
            return Children.FirstOrDefault(x => x.Name == name);
        }

        /// <summary>
        /// Returns the command with the matching name.
        /// </summary>
        /// <param name="name">The name of the command to look for.</param>
        /// <returns>The corresponding command, or <c>null</c> if no command with the given name exists.</returns>
        [CanBeNull]
        public ICommandBase GetCommand(string name)
        {
            name = EscapeName(name);
            return Commands.FirstOrDefault(x => x.Name == name);
        }

        /// <summary>
        /// Returns the additionnal data with the matching name.
        /// </summary>
        /// <param name="name">The name of the additionnal data to look for.</param>
        /// <returns>The corresponding additionnal data, or <c>null</c> if no data with the given name exists.</returns>
        public object GetAssociatedData(string name)
        {
            name = EscapeName(name);
            return AssociatedData.FirstOrDefault(x => x.Key == name).Value;
        }

        /// <summary>
        /// Returns the child node, the command or the additional data with the matching name.
        /// </summary>
        /// <param name="name">The name of the object to look for.</param>
        /// <returns>The corresponding object, or <c>null</c> if no object with the given name exists.</returns>
        public object GetDynamicObject(string name)
        {
            name = EscapeName(name);
            return GetChild(name) ?? GetCommand(name) ?? GetAssociatedData(name) ?? DependencyProperty.UnsetValue;
        }

        /// <inheritdoc/>
        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new NodeViewModelDynamicMetaObject(parameter, this);
        }

        protected void NotifyPropertyChanging(string propertyName)
        {
            if (!changingProperties.Contains(propertyName))
            {
                changingProperties.Add(propertyName);
                OnPropertyChanging(propertyName);
            }
        }

        protected void NotifyPropertyChanged(string propertyName)
        {
            if (changingProperties.Remove(propertyName))
            {
                OnPropertyChanged(propertyName);
            }
        }

        [NotNull]
        protected virtual NodePresenterCommandWrapper ConstructCommandWrapper([NotNull] INodePresenterCommand command)
        {
            return new NodePresenterCommandWrapper(ServiceProvider, nodePresenters, command);
        }

        protected virtual object GetNodeValue()
        {
            object currentValue = null;
            var isFirst = true;
            foreach (var nodePresenter in NodePresenters)
            {
                if (isFirst)
                {
                    currentValue = nodePresenter.Value;
                }
                else if (nodePresenter.Factory.IsPrimitiveType(nodePresenter.Value?.GetType()))
                {
                    if (!AreValueEqual(currentValue, nodePresenter.Value))
                        return DifferentValues;
                }
                else
                {
                    if (!AreValueEquivalent(currentValue, nodePresenter.Value))
                        return DifferentValues;
                }
                isFirst = false;
            }
            return currentValue;
        }

        protected virtual void SetNodeValue(object newValue)
        {
            foreach (var nodePresenter in NodePresenters)
            {
                // TODO: normally it shouldn't take that path (since it uses commands), but this is not safe with newly instantiated values
                // fixme adding a test to check whether it's a content type from Quantum point of view might be safe enough.
                var oldValue = nodePresenter.Value;
                if (!Equals(oldValue, newValue))
                {
                    nodePresenter.UpdateValue(newValue);
                }
            }
        }

        /// <summary>
        /// Refreshes this node, updating its properties and its child nodes.
        /// </summary>
        public void Refresh()
        {
            foreach (var child in Children.ToList())
            {
                RemoveChild(child);
                child.Destroy();
            }
            foreach (var command in Commands.ToList())
            {
                RemoveCommand(command);
            }
            foreach (var data in AssociatedData.ToList())
            {
                RemoveAssociatedData(data.Key);
            }

            owner.GraphViewModelService.NodeViewModelFactory.GenerateChildren(owner, this, nodePresenters);
            FinishInitialization();
        }

        protected void CheckDynamicMemberConsistency()
        {
            var memberNames = new HashSet<string>();
            foreach (var child in Children)
            {
                if (string.IsNullOrWhiteSpace(child.Name))
                    throw new InvalidOperationException("This node has a child with a null or blank name");

                if (child.Name.Contains('.'))
                    throw new InvalidOperationException($"This node has a child which contains a period (.) in its name: {child.Name}");

                if (memberNames.Contains(child.Name))
                    throw new InvalidOperationException($"This node contains several members named {child.Name}");

                memberNames.Add(child.Name);
            }

            foreach (var command in Commands)
            {
                if (string.IsNullOrWhiteSpace(command.Name))
                    throw new InvalidOperationException("This node has a command with a null or blank name {0}");

                if (memberNames.Contains(command.Name))
                    throw new InvalidOperationException($"This node contains several members named {command.Name}");

                memberNames.Add(command.Name);
            }

            foreach (var associatedDataKey in AssociatedData.Keys)
            {
                if (string.IsNullOrWhiteSpace(associatedDataKey))
                    throw new InvalidOperationException("This node has associated data with a null or blank name {0}");

                if (memberNames.Contains(associatedDataKey))
                    throw new InvalidOperationException($"This node contains several members named {associatedDataKey}");

                memberNames.Add(associatedDataKey);
            }
        }

        protected override void OnPropertyChanging(params string[] propertyNames)
        {
            if (DebugQuantumPropertyChanges && HasPropertyChangingSubscriber)
            {
                foreach (var property in propertyNames)
                {
                    owner.Logger.Debug($"Node Property changing: [{Path}].{property}");
                }
            }
            base.OnPropertyChanging(propertyNames);
        }

        protected override void OnPropertyChanged(params string[] propertyNames)
        {
            if (DebugQuantumPropertyChanges && HasPropertyChangedSubscriber)
            {
                foreach (var property in propertyNames)
                {
                    owner.Logger.Debug($"Node Property changed: [{Path}].{property}");
                }
            }
            base.OnPropertyChanged(propertyNames);
        }

        /// <summary>
        /// Indicates whether two values of <see cref="INodePresenter"/> are considered equal.
        /// This method is used to compare values of primitive types to decide if this view model should use <see cref="DifferentValues"/>.
        /// </summary>
        /// <param name="value1">The first value to compare.</param>
        /// <param name="value2">The second value to compare.</param>
        /// <returns>True if both values are considered equal, false otherwise.</returns>
        protected virtual bool AreValueEqual(object value1, object value2)
        {
            return Equals(value1, value2);
        }

        /// <summary>
        /// Indicates whether two values of <see cref="INodePresenter"/> are considered equivalent.
        /// This method is used to compare values that are not primitive types to decide if this view model should use <see cref="DifferentValues"/>.
        /// </summary>
        /// <param name="value1">The first value to compare.</param>
        /// <param name="value2">The second value to compare.</param>
        /// <returns>True if both values are considered equivalent, false otherwise.</returns>
        protected virtual bool AreValueEquivalent(object value1, object value2)
        {
            return value1?.GetType() == value2?.GetType();
        }

        private void ValueChanging(object sender, ValueChangingEventArgs valueChangingEventArgs)
        {
            OnValueChanging();
        }

        private void ValueChanged(object sender, ValueChangedEventArgs valueChangedEventArgs)
        {
            OnValueChanged();
        }

        private void AttachedPropertyUpdated(ref PropertyContainer propertycontainer, PropertyKey propertykey, object newvalue, object oldvalue)
        {
            // This is the only legit scenario where we don't want re-entrancy. valueChanging should not be tested directly in one
            // of those methods, because other cases could actually be problem.
            if (!valueChanging)
            {
                OnValueChanging();
                OnValueChanged();
            }
        }

        private void OnValueChanging()
        {
            valueChanging = true;
            Parent?.NotifyPropertyChanging(Name);
            OnPropertyChanging(nameof(NodeValue));
        }

        private void OnValueChanged()
        {
            Parent?.NotifyPropertyChanged(Name);

            OnPropertyChanging(nameof(VisibleChildrenCount));
            // This node can have been disposed by its parent already (if its parent is being refreshed and share the same source node)
            // In this case, let's trigger the notifications gracefully before being discarded, but skip refresh
            if (!IsDestroyed)
            {
                Refresh();
            }
            UpdateViewModelProperties();
            OnPropertyChanged(nameof(VisibleChildrenCount));

            OnPropertyChanged(nameof(NodeValue));
            owner.NotifyNodeChanged(this);

            valueChanging = false;
        }

        private object ConvertValue(object value)
        {
            if (value == null)
                return null;
            object convertedValue;
            if (!TypeConverterHelper.TryConvert(value, Type, out convertedValue))
                throw new InvalidCastException("Can not convert value to the required type");
            return convertedValue;
        }

        private void AddChild([NotNull] NodeViewModel child) => ChangeAndNotify(() => { child.Parent = this; ((ICollection<NodeViewModel>)initializingChildren ?? children).Add(child); }, $"{GraphViewModel.HasChildPrefix}{child.Name}", child.Name);

        private void RemoveChild([NotNull] NodeViewModel child) => ChangeAndNotify(() => { child.Parent = null; ((ICollection<NodeViewModel>)initializingChildren ?? children).Remove(child); }, $"{GraphViewModel.HasChildPrefix}{child.Name}", child.Name);

        private void AddCommand([NotNull] NodePresenterCommandWrapper command) => ChangeAndNotify(() => commands.Add(command), $"{GraphViewModel.HasCommandPrefix}{command.Name}", command.Name);

        private void RemoveCommand([NotNull] NodePresenterCommandWrapper command) => ChangeAndNotify(() => commands.Remove(command), $"{GraphViewModel.HasCommandPrefix}{command.Name}", command.Name);

        private void AddAssociatedData([NotNull] string key, object value) => ChangeAndNotify(() => associatedData.Add(key, value), $"{GraphViewModel.HasAssociatedDataPrefix}{key}", key);

        private void RemoveAssociatedData([NotNull] string key) => ChangeAndNotify(() => associatedData.Remove(key), $"{GraphViewModel.HasAssociatedDataPrefix}{key}", key);

        private void ChangeAndNotify([NotNull] Action changeAction, params string[] propertyNames)
        {
            if (changeAction == null) throw new ArgumentNullException(nameof(changeAction));
            if (initializingChildren == null)
            {
                foreach (var propertyName in propertyNames)
                    NotifyPropertyChanging(propertyName);
            }
            changeAction();
            if (initializingChildren == null)
            {
                foreach (var propertyName in propertyNames)
                    NotifyPropertyChanged(propertyName);
            }
        }

        private void UpdateViewModelProperties()
        {
            var shouldBeVisible = false;
            var shouldBeReadOnly = false;

            foreach (var nodePresenter in nodePresenters)
            {
                // Display this node if at least one presenter is visible
                if (nodePresenter.IsVisible)
                    shouldBeVisible = true;

                // Make it read-only if at least one presenter is read-only
                if (nodePresenter.IsReadOnly)
                    shouldBeReadOnly = true;
            }

            IsVisible = shouldBeVisible;
            IsReadOnly = shouldBeReadOnly;

            // TODO: find a way to "merge" display name if they are different (string.Join?)
            DisplayName = Utils.SplitCamelCase(nodePresenters.First().DisplayName);

            CheckDynamicMemberConsistency();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Name}: [{GetNodeValue()}]";
        }

        /// <summary>
        /// Indicates whether the given name is reserved for the name of a property in an <see cref="Xenko.Core.Presentation.Quantum.ViewModels.NodeViewModel"/>. Any children node with a colliding name will
        /// be escaped with the <see cref="EscapeName"/> method.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns><c>True</c> if the name is reserved, <c>false</c> otherwise.</returns>
        public static bool IsReserved(string name)
        {
            return ReservedNames.Contains(name);
        }

        /// <summary>
        /// Escapes the name of a child to avoid name collision with a property.
        /// </summary>
        /// <param name="name">The name to escape.</param>
        /// <returns>The escaped name.</returns>
        /// <remarks>Names are escaped using a trailing underscore character.</remarks>
        [NotNull]
        public static string EscapeName(string name)
        {
            var escaped = !IsReserved(name) ? name : name + "_";
            return escaped.Replace(".", "-");
        }

        private static int CompareChildren([NotNull] NodeViewModel a, [NotNull] NodeViewModel b)
        {
            // Order has the best priority for comparison, if set.
            if ((a.Order ?? 0) != (b.Order ?? 0))
                return (a.Order ?? 0).CompareTo(b.Order ?? 0);

            // Then, try to use metadata token (if members)
            if (a.MemberInfo != null || b.MemberInfo != null)
            {
                var comparison = a.MemberInfo.CompareMetadataTokenWith(b.MemberInfo);
                if (comparison != 0)
                    return comparison;
            }

            // Then we use name, only if both orders are unset.
            if (a.Order == null && b.Order == null)
            {
                return string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase);
            }

            // Otherwise, the first child would be the one who have an order value.
            return a.Order == null ? 1 : -1;
        }

        [CanBeNull]
        private static object DefaultCombineAttachedProperty([NotNull] IEnumerable<object> arg)
        {
            object result = null;
            bool isFirst = true;
            foreach (var value in arg)
            {
                if (isFirst)
                    result = value;
                else
                    result = Equals(result, value) ? result : DifferentValues;
                isFirst = false;
            }
            return result;
        }
    }
}
