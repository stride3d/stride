// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Interactivity;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.Interactivity
{
    /// <summary>
    /// A collection of behavior that synchronize with the System.Windows.Interactivity.Interaction.Behaviors attached property.
    /// </summary>
    public class BehaviorCollection : ObservableCollection<Behavior>, IAttachedObject
    {
        public BehaviorCollection()
        {
            var collection = (INotifyCollectionChanged)this;
            collection.CollectionChanged += BehaviorCollectionChanged;
        }

        public DependencyObject AssociatedObject { get; private set; }

        [NotNull]
        public BehaviorCollection Clone()
        {
            var clone = new BehaviorCollection();
            foreach (var behavior in Items)
            {
                clone.Add((Behavior)behavior.Clone());
            }
            return clone;
        }

        public void Attach([NotNull] DependencyObject dependencyObject)
        {
            if (dependencyObject == null) throw new ArgumentNullException(nameof(dependencyObject));
            // Aleady attached
            if (ReferenceEquals(AssociatedObject, dependencyObject))
                return;

            if (AssociatedObject != null)
                throw new InvalidOperationException("This BehaviorCollection has already been attached to a dependency object.");

            AssociatedObject = dependencyObject;
            var behaviors = System.Windows.Interactivity.Interaction.GetBehaviors(dependencyObject);
            foreach (var behavior in this)
            {
                behaviors.Add(behavior);
            }
        }

        public void Detach()
        {
            if (AssociatedObject != null)
            {
                var behaviors = System.Windows.Interactivity.Interaction.GetBehaviors(AssociatedObject);
                foreach (var behavior in this)
                {
                    behaviors.Remove(behavior);
                }
            }
            AssociatedObject = null;
        }

        private void BehaviorCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (AssociatedObject == null)
                return;

            var behaviors = System.Windows.Interactivity.Interaction.GetBehaviors(AssociatedObject);
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Behavior newItem in e.NewItems)
                        behaviors.Add(newItem);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Behavior oldItem in e.OldItems)
                        behaviors.Remove(oldItem);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (Behavior oldItem in e.OldItems)
                        behaviors.Remove(oldItem);
                    foreach (Behavior newItem in e.NewItems)
                        behaviors.Add(newItem);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    behaviors.Clear();
                    foreach (Behavior newItem in e.NewItems)
                        behaviors.Add(newItem);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
