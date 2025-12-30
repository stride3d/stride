// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Behaviors;

/// <summary>
/// Allows the bind the <see cref="ToolTip.TipProperty"/> of a control to a particular target property when the attached control is hovered by the mouse.
/// This behavior can be used to display the same message that the tool-tip in a status bar, for example.
/// </summary>
/// <remarks>This behavior can be used to display the tool tip of some controls in another place, such as a status bar.</remarks>
public class BindCurrentToolTipStringBehavior : Behavior<Control>
{
    /// <summary>
    /// Identifies the <see cref="ToolTipTarget"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<string?> ToolTipTargetProperty =
        AvaloniaProperty.Register<BindCurrentToolTipStringBehavior, string?>(nameof(ToolTipTarget), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Identifies the <see cref="DefaultValue"/> dependency property.
    /// </summary>
    public static readonly StyledProperty<string?> DefaultValueProperty =
        AvaloniaProperty.Register<BindCurrentToolTipStringBehavior, string?>(nameof(DefaultValue), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the tool tip text of the control when the mouse is over the control, or <see cref="DefaultValue"/> otherwise. This property should usually be bound.
    /// </summary>
    public string? ToolTipTarget
    {
        get => GetValue(ToolTipTargetProperty);
        set => SetValue(ToolTipTargetProperty, value);
    }

    /// <summary>
    /// Gets or sets the default value to set when the mouse is not over the control.
    /// </summary>
    public string? DefaultValue
    {
        get => GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is not null)
        {
            AssociatedObject.PointerEntered += MouseEnter;
            AssociatedObject.PointerExited += MouseLeave;
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject is not null)
        {
            AssociatedObject.PointerEntered -= MouseEnter;
            AssociatedObject.PointerExited -= MouseLeave;
        }
        base.OnDetaching();
    }

    private void MouseEnter(object? sender, PointerEventArgs e)
    {
        if (AssociatedObject is not null)
        {
            SetCurrentValue(ToolTipTargetProperty, ToolTip.GetTip(AssociatedObject));
        }
    }

    private void MouseLeave(object? sender, PointerEventArgs e)
    {
        SetCurrentValue(ToolTipTargetProperty, DefaultValue);
    }
}
