// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;

namespace Stride.Core.Presentation.Core
{
    public class ValidationRoutedEventArgs : RoutedEventArgs
    {
        public object Value { get; }

        public ValidationRoutedEventArgs(RoutedEvent routedEvent, object value)
            : base(routedEvent)
        {
            Value = value;
        }
    }

    public class ValidationRoutedEventArgs<T> : ValidationRoutedEventArgs
    {
        public new T Value => (T)base.Value;

        public ValidationRoutedEventArgs(RoutedEvent routedEvent, T value)
            : base(routedEvent, value)
        {
        }
    }

    public delegate void ValidationRoutedEventHandler(object sender, ValidationRoutedEventArgs e);

    public delegate void ValidationRoutedEventHandler<T>(object sender, ValidationRoutedEventArgs<T> e);
}
