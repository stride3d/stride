// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Behaviors;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public abstract class ActivateOnLocationChangedBehavior<T> : ActivateOnCollectionChangedBehavior<T> where T : DependencyObject
    {
        private bool selectionDone;

        protected override bool MatchChange(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                selectionDone = false;
            }
            if (e.Action == NotifyCollectionChangedAction.Add && !selectionDone)
            {
                if (e.NewItems.OfType<DirectoryBaseViewModel>().Any())
                {
                    selectionDone = true;
                }
            }
            return selectionDone;
        }
    }
}
