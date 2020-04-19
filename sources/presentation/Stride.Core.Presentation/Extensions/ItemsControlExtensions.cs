// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Extensions
{
    public static class ItemsControlExtensions
    {
        [CanBeNull]
        public static ItemsControl GetParentContainer([NotNull] this ItemsControl itemsControl)
        {
            var parent = VisualTreeHelper.GetParent(itemsControl);

            while (parent != null && (parent is ItemsControl) == false)
                parent = VisualTreeHelper.GetParent(parent);

            return parent as ItemsControl;
        }

        public static IEnumerable<ItemsControl> GetChildContainers([NotNull] this ItemsControl itemsControl)
        {
            var gen = itemsControl.ItemContainerGenerator;

            foreach (var item in gen.Items)
            {
                var container = gen.ContainerFromItem(item) as ItemsControl;
                if (container != null)
                    yield return container;
            }
        }
    }
}
