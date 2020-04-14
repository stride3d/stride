// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.UI.Events;

namespace Stride.UI.Tests.Events
{
    internal class MyTestRoutedEventArgs : RoutedEventArgs
    {
        public MyTestRoutedEventArgs(RoutedEvent routedEvent)
            : base(routedEvent)
        {
        }
    }
}
