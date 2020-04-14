// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum
{
    /// <summary>
    /// This class allows to bind a property of a view model to an <see cref="IMemberNode"/> and properly trigger property change notifications
    /// when the node value is modified.
    /// </summary>
    /// <typeparam name="TTargetType">The type of property bound to the graph node.</typeparam>
    /// <typeparam name="TContentType">The type of content in the graph node.</typeparam>
    public class MemberGraphNodeBinding<TTargetType, TContentType> : GraphNodeBinding<TTargetType, TContentType>
    {
        protected IMemberNode Node;

        public MemberGraphNodeBinding([NotNull] IMemberNode node, string propertyName, PropertyChangeDelegate propertyChanging, PropertyChangeDelegate propertyChanged, [NotNull] Func<TTargetType, TContentType> converter, IUndoRedoService actionService, bool notifyChangesOnly = true)
            : base(propertyName, propertyChanging, propertyChanged, converter, actionService, notifyChangesOnly)
        {
            Node = node;
            node.ValueChanged += ValueChanged;
            node.ValueChanging += ValueChanging;
        }

        public override void Dispose()
        {
            base.Dispose();
            Node.ValueChanged -= ValueChanged;
            Node.ValueChanging -= ValueChanging;
        }

        public override TContentType GetNodeValue()
        {
            var value = (TContentType)Node.Retrieve();
            return value;
        }

        public override void SetNodeValue(TTargetType value)
        {
            using (var transaction = ActionService?.CreateTransaction())
            {
                Node.Update(Converter(value));
                ActionService?.SetName(transaction, $"Update property {PropertyName}");
            }
        }
    }

    /// <summary>
    /// This is a specialization of the <see cref="MemberGraphNodeBinding{TTargetType,TContentType}"/> class, when the target type is the same that the
    /// content type.
    /// This class allows to bind a property of a view model to a <see cref="IMemberNode"/> and properly trigger property change notifications
    /// when the node value is modified.
    /// </summary>
    /// <typeparam name="TContentType">The type of the node content and the property bound to the graph node.</typeparam>
    public class MemberGraphNodeBinding<TContentType> : MemberGraphNodeBinding<TContentType, TContentType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberGraphNodeBinding{TContentType}"/> class.
        /// </summary>
        /// <param name="node">The graph node bound to this instance.</param>
        /// <param name="propertyName">The name of the property of the view model that is bound to this instance.</param>
        /// <param name="propertyChanging">The delegate to invoke when the node content is about to change.</param>
        /// <param name="propertyChanged">The delegate to invoke when the node content has changed.</param>
        /// <param name="actionService"></param>
        /// <param name="notifyChangesOnly">If <c>True</c>, delegates will be invoked only if the content of the node has actually changed. Otherwise, they will be invoked every time the node is updated, even if the new value is equal to the previous one.</param>
        public MemberGraphNodeBinding([NotNull] IMemberNode node, string propertyName, PropertyChangeDelegate propertyChanging, PropertyChangeDelegate propertyChanged, IUndoRedoService actionService, bool notifyChangesOnly = true)
            : base(node, propertyName, propertyChanging, propertyChanged, x => x, actionService, notifyChangesOnly)
        {
        }

        /// <summary>
        /// Gets or sets the current node value.
        /// </summary>
        /// <remarks>The setter of this property will invoke the delegates passed to the constructor of this instance if the new value is different from the previous one.</remarks>
        public TContentType Value { get => GetNodeValue(); set => SetNodeValue(value); }
    }
}
