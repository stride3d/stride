// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;

namespace Stride.Core.Presentation.Avalonia.Windows;

/// <summary>
/// Extra content of the title bar drawn by TitleBar.axaml, left of the window buttons.
/// </summary>
/// <remarks>
/// The title bar is drawn above the window content, so a control of the content can't receive clicks there: the title
/// bar would take them to move the window. The template shows this content in the title bar itself, as an interactive
/// element.
/// </remarks>
public static class WindowTitleBar
{
    public static readonly AttachedProperty<Control?> ContentProperty =
        AvaloniaProperty.RegisterAttached<Window, Control?>("Content", typeof(WindowTitleBar));

    public static Control? GetContent(Window window) => window.GetValue(ContentProperty);

    public static void SetContent(Window window, Control? value) => window.SetValue(ContentProperty, value);
}
