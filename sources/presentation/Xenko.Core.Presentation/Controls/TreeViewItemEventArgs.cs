// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;

namespace Xenko.Core.Presentation.Controls
{
    public class TreeViewItemEventArgs : RoutedEventArgs
    {
        public TreeViewItem Container { get; private set; }

        public object Item { get; private set; }

        public TreeViewItemEventArgs(RoutedEvent routedEvent, object source, TreeViewItem container, object item)
            : base(routedEvent, source)
        {
            Container = container;
            Item = item;
        }
    }
}
