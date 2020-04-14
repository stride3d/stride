// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// A service that manages selection and record history to allow navigating backward/forward.
    /// </summary>
    public class SelectionService
    {
        private readonly IDispatcherService dispatcher;
        private readonly List<SelectionScope> scopes = new List<SelectionScope>();
        private readonly ITransactionStack stack = TransactionStackFactory.Create(int.MaxValue);
        private ITransaction currentTransaction;

        /// <summary>
        /// The current state of the selection.
        /// </summary>
        internal SelectionState CurrentState = new SelectionState();

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionService"/> class.
        /// </summary>
        /// <param name="dispatcher"></param>
        public SelectionService(IDispatcherService dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        /// <summary>
        /// Gets whether a change in a selection collection should be ignored.
        /// </summary>
        public bool PropertySelectionSuppressed { get; private set; }

        /// <summary>
        /// Gets whether it is possible to navigate backward in the selection history.
        /// </summary>
        public bool CanGoBack => stack.CanRollback;

        /// <summary>
        /// Gets whether it is possible to navigate forward in the selection history.
        /// </summary>
        public bool CanGoForward => stack.CanRollforward;

        /// <summary>
        /// Navigates backward in the selection history.
        /// </summary>
        public void NavigateBackward()
        {
            // Should hardly happen, but let's ignore this action if we're still terminating a selection
            if (currentTransaction != null)
                return;

            var initialState = CurrentState;
            while (stack.CanRollback && (initialState.Equals(CurrentState) || !CurrentState.HasValidSelection()))
            {
                stack.Rollback();
                // Skip transactions that don't have effect anymore
            }
        }

        /// <summary>
        /// Navigates forward in the selection history.
        /// </summary>
        public void NextSelection()
        {
            // Should hardly happen, but let's ignore this action if we're still terminating a selection
            if (currentTransaction != null)
                return;

            var initialState = CurrentState;
            while (stack.CanRollforward && (initialState.Equals(CurrentState) || !CurrentState.HasValidSelection()))
            {
                stack.Rollforward();
                // Skip transactions that don't have effect anymore
            }
        }

        /// <summary>
        /// Registers collections of selected objects to the <see cref="SelectionService"/>.
        /// </summary>
        /// <param name="idToObject">A function used to resolve an identifier to an object to select when restoring the selection..</param>
        /// <param name="objectToId">A function used to retrieve the identifier of an object that is part of the current selection.</param>
        /// <param name="collections">The collection scope to register.</param>
        /// <returns>The scope corresponding to the collections.</returns>
        [NotNull]
        public SelectionScope RegisterSelectionScope([NotNull] Func<AbsoluteId, object> idToObject, [NotNull] Func<object, AbsoluteId?> objectToId, [NotNull] params INotifyCollectionChanged[] collections)
        {
            if (idToObject == null) throw new ArgumentNullException(nameof(idToObject));
            if (objectToId == null) throw new ArgumentNullException(nameof(objectToId));
            if (collections == null) throw new ArgumentNullException(nameof(collections));

            var scope = new SelectionScope(collections, idToObject, objectToId);
            foreach (var collection in scope.Collections)
                collection.CollectionChanged += CollectionChanged;
            scopes.Add(scope);
            // Update history to let it know about this collection
            foreach (var operation in stack.RetrieveAllTransactions().SelectMany(x => x.Operations).OfType<SelectionOperation>())
            {
                foreach (var collection in scope.Collections)
                    operation.RegisterCollection(scope, collection);
            }
            // Also update the current state
            foreach (var collection in scope.Collections)
                CurrentState.RegisterCollection(scope, collection);

            return scope;
        }

        /// <summary>
        /// Unregisters collections of selected objects from the <see cref="SelectionService"/>.
        /// </summary>
        /// <param name="collections"></param>
        /// <remarks>The order of the collections to unregister must match the order used when registering with <see cref="RegisterSelectionScope"/>.</remarks>
        public void UnregisterSelectionScope(params INotifyCollectionChanged[] collections)
        {
            foreach (var scope in scopes)
            {
                if (ArrayExtensions.ArraysReferenceEqual(scope.Collections, collections))
                {
                    UnregisterSelectionScope(scope);
                    return;
                }
            }

            throw new ArgumentException($"Provided {nameof(collections)} don't match an existing scope", nameof(collections));
        }

        /// <summary>
        /// Unregisters a collection scope selected objects from the <see cref="SelectionService"/>.
        /// </summary>
        /// <param name="scope">The selection scope to unregister.</param>
        public void UnregisterSelectionScope([NotNull] SelectionScope scope)
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            foreach (var collection in scope.Collections)
                collection.CollectionChanged -= CollectionChanged;
            scopes.Remove(scope);
            // Update history to let it know about this collection
            foreach (var operation in stack.RetrieveAllTransactions().SelectMany(x => x.Operations).OfType<SelectionOperation>())
            {
                foreach (var collection in scope.Collections)
                    operation.UnregisterCollection(collection);
            }
            // Also update the current state
            foreach (var collection in scope.Collections)
                CurrentState.UnregisterCollection(collection);
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (stack.RollInProgress)
                return;

            // We create a transaction at the first change, and complete it at the next dispatcher frame with InvokeAsync
            if (currentTransaction == null)
            {
                currentTransaction = stack.CreateTransaction();
                dispatcher.InvokeAsync(CompleteTransaction);
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                PropertySelectionSuppressed = true;
                try
                {
                    // When we add a new item to selection, we clear the selection in all other collections
                    foreach (var scope in scopes.Where(x => !x.Collections.Contains(sender)))
                    {
                        foreach (var collection in scope.Collections)
                        {
                            var descriptor = TypeDescriptorFactory.Default.Find(collection.GetType()) as CollectionDescriptor;
                            descriptor?.Clear(collection);
                        }
                    }
                }
                finally
                {
                    PropertySelectionSuppressed = false;
                }
            }
        }

        private void CompleteTransaction()
        {
            if (currentTransaction == null)
                throw new InvalidOperationException("Trying to complete a selection transaction but no transaction in progress");

            var newState = new SelectionState();
            newState.SaveStates(scopes);

            currentTransaction.Continue();
            stack.PushOperation(new SelectionOperation(this, CurrentState, newState));
            currentTransaction.Complete();
            currentTransaction = null;

            CurrentState = newState;
        }
    }
}
