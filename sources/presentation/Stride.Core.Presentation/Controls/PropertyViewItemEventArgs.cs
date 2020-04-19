// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;

namespace Stride.Core.Presentation.Controls
{
    public class PropertyViewItemEventArgs : RoutedEventArgs
    {
        public PropertyViewItem Container { get; private set; }

        public object Item { get; private set; }

        public PropertyViewItemEventArgs(RoutedEvent routedEvent, object source, PropertyViewItem container, object item)
            : base(routedEvent, source)
        {
            Container = container;
            Item = item;
        }
    }
}
