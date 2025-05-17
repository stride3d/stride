// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class PropertyViewItemEventArgs : RoutedEventArgs
{
    public PropertyViewItem Container { get; private set; }

    public PropertyViewItemEventArgs(RoutedEvent routedEvent, object source, PropertyViewItem container)
        : base(routedEvent, source)
    {
        Container = container;
    }
}
