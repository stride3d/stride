// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Behaviors;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    /// <summary>
    /// The base class for a behavior that allows to activate the associated dependency object when an observable collection of <see cref="ILogMessage"/>
    /// receives a new message which has an equal or greater level comparing to the <see cref="MinimumLevel"/> of this behavior.
    /// </summary>
    /// <typeparam name="T">The type of dependency object associated to this behavior.</typeparam>
    public abstract class ActivateOnLogBehavior<T> : ActivateOnCollectionChangedBehavior<T> where T : DependencyObject
    {
        private bool selectionDone;

        /// <summary>
        /// Identifies the <see cref="MinimumLevel"/> dependency property.
        /// </summary>
        public static DependencyProperty MinimumLevelProperty = DependencyProperty.Register("MinimumLevel", typeof(LogMessageType),
            typeof(ActivateOnLogBehavior<T>), new FrameworkPropertyMetadata(LogMessageType.Debug));

        /// <summary>
        /// Gets or sets the minimum level of message to receive in order to trigger activation of the associated control.
        /// </summary>
        public LogMessageType MinimumLevel
        {
            get { return (LogMessageType)GetValue(MinimumLevelProperty); }
            set { SetValue(MinimumLevelProperty, value); }
        }

        protected override bool MatchChange(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                selectionDone = false;
            }
            if (e.Action == NotifyCollectionChangedAction.Add && !selectionDone)
            {
                if (e.NewItems.OfType<ILogMessage>().Any(x => x.IsAtLeast(MinimumLevel)))
                {
                    selectionDone = true;
                }
            }
            return selectionDone;
        }
    }

    /// <summary>
    /// An implementation of the <see cref="ActivateOnLogBehavior{T}"/> for the <see cref="TabItem"/> control.
    /// </summary>
    public class TabItemActivateOnLogBehavior : ActivateOnLogBehavior<TabItem>
    {
        protected override void Activate()
        {
            AssociatedObject.IsSelected = true;
        }
    }

}
