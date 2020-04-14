// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Specialized;
using Stride.Core.Annotations;
using Stride.Core.Transactions;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// An operation consisting of switching the selection from one <see cref="SelectionState"/> to another.
    /// </summary>
    public class SelectionOperation : Operation
    {
        private readonly SelectionService service;
        private readonly SelectionState previousState;
        private readonly SelectionState nextState;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionOperation"/> class.
        /// </summary>
        /// <param name="service">The selection service creating this operation.</param>
        /// <param name="previousState">The state of selection before this operation.</param>
        /// <param name="nextState">The state of selection after this operation.</param>
        public SelectionOperation(SelectionService service, SelectionState previousState, SelectionState nextState)
        {
            this.service = service;
            this.previousState = previousState;
            this.nextState = nextState;
        }

        /// <summary>
        /// Registers a collection to the previous and next states of this operation.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="collection">The collection to register.</param>
        public void RegisterCollection([NotNull] SelectionScope scope, [NotNull] INotifyCollectionChanged collection)
        {
            previousState.RegisterCollection(scope, collection);
            nextState.RegisterCollection(scope, collection);
        }

        /// <summary>
        /// Unregisters a collection from the previous and next states of this operation.
        /// </summary>
        /// <param name="collection">The collection to unregister.</param>
        public void UnregisterCollection([NotNull] INotifyCollectionChanged collection)
        {
            previousState.UnregisterCollection(collection);
            nextState.UnregisterCollection(collection);
        }

        /// <inheritdoc/>
        protected override void Rollback()
        {
            previousState.RestoreStates();
            service.CurrentState = previousState;
        }

        /// <inheritdoc/>
        protected override void Rollforward()
        {
            nextState.RestoreStates();
            service.CurrentState = nextState;
        }
    }
}
