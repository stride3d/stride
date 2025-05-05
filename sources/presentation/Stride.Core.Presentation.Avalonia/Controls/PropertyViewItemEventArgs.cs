// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
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
