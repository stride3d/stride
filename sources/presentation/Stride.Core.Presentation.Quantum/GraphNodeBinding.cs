// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum
{
    public abstract class GraphNodeBinding<TTargetType, TContentType> : IDisposable
    {
        public delegate void PropertyChangeDelegate(string[] propertyNames);

        protected readonly IUndoRedoService ActionService;
        protected readonly string PropertyName;
        protected readonly Func<TTargetType, TContentType> Converter;

        private readonly PropertyChangeDelegate propertyChanging;
        private readonly PropertyChangeDelegate propertyChanged;
        private readonly bool notifyChangesOnly;

        internal GraphNodeBinding(string propertyName, PropertyChangeDelegate propertyChanging, PropertyChangeDelegate propertyChanged, [NotNull] Func<TTargetType, TContentType> converter, IUndoRedoService actionService, bool notifyChangesOnly = true)
        {
            PropertyName = propertyName;
            this.propertyChanging = propertyChanging;
            this.propertyChanged = propertyChanged;
            Converter = converter ?? throw new ArgumentNullException(nameof(converter));
            ActionService = actionService;
            this.notifyChangesOnly = notifyChangesOnly;
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Gets the current value of the graph node.
        /// </summary>
        /// <returns>The current value of the graph node.</returns>
        /// <remarks>This method can be invoked from a property getter.</remarks>
        public abstract TContentType GetNodeValue();

        /// <summary>
        /// Sets the current value of the graph node.
        /// </summary>
        /// <param name="value">The value to set for the graph node content.</param>
        /// <remarks>This method can be invoked from a property setter.</remarks>
        /// <remarks>This method will invoke the delegates passed to the constructor of this instance if the new value is different from the previous one.</remarks>
        public abstract void SetNodeValue(TTargetType value);

        protected void ValueChanging(object sender, INodeChangeEventArgs e)
        {
            if (!notifyChangesOnly || !Equals(e.OldValue, e.NewValue))
            {
                propertyChanging?.Invoke(new[] { PropertyName });
            }
        }

        protected void ValueChanged(object sender, INodeChangeEventArgs e)
        {
            if (!notifyChangesOnly || !Equals(e.OldValue,e.NewValue))
            {
                propertyChanged?.Invoke(new[] { PropertyName });
            }
        }
    }
}
