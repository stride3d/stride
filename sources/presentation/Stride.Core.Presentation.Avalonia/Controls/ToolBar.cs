// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class ToolBar : ItemsControl
{
    /// <summary>
    /// Defines the <see cref="Orientation"/> property.
    /// </summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.RegisterAttached<ToolBar, Orientation>(nameof(Orientation), typeof(ToolBar), coerce: CoerceOrientation);

    private static Orientation CoerceOrientation(AvaloniaObject d, Orientation value)
    {
        ToolBarTray? toolBarTray = ((ToolBar)d).ToolBarTray;
        return toolBarTray?.Orientation ?? value;
    }

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
    }

    private ToolBarTray? ToolBarTray => Parent as ToolBarTray;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        CoerceValue(OrientationProperty);
    }
}
