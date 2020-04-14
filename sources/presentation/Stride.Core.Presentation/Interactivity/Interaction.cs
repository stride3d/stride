// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.Interactivity
{
    /// <summary>
    /// A container for an attached dependency property that contains a collection of behavior.
    /// The purpose of this class is to be used in place of Microsoft.Xaml.Behaviors.Interaction.
    /// This class allows to set behaviors in styles because <see cref="BehaviorCollection"/>
    /// has a public parameterless constructor and the Behaviors attached property has a public setter.
    /// When the collection is modified or set, it automatically synchronize the attached property
    /// Microsoft.Xaml.Behaviors.Interaction.Behaviors.
    /// </summary>
    public static class Interaction
    {
        public static readonly DependencyProperty BehaviorsProperty = DependencyProperty.RegisterAttached("Behaviors", typeof(BehaviorCollection), typeof(Interaction), new PropertyMetadata(new BehaviorCollection(), OnBehaviorCollectionChanged));

        public static BehaviorCollection GetBehaviors([NotNull] DependencyObject obj)
        {
            return (BehaviorCollection)obj.GetValue(BehaviorsProperty);
        }

        public static void SetBehaviors([NotNull] DependencyObject obj, BehaviorCollection value)
        {
            obj.SetValue(BehaviorsProperty, value);
        }

        private static void OnBehaviorCollectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (BehaviorCollection)e.OldValue;
            oldValue?.Detach();

            var newValue = (BehaviorCollection)e.NewValue;
            if (newValue != null)
            {
                if (newValue.AssociatedObject != null)
                    newValue = newValue.Clone();

                newValue.Attach(d);
            }
        }
    }
}
