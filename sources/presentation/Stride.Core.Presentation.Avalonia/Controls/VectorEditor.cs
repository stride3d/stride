// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Data;

namespace Stride.Core.Presentation.Avalonia.Controls;

public abstract class VectorEditor<T> : VectorEditorBase<T>
{
    public static readonly StyledProperty<VectorEditingMode> EditingModeProperty =
        AvaloniaProperty.Register<VectorEditor<T>, VectorEditingMode>(nameof(EditingMode), defaultBindingMode: BindingMode.TwoWay);

    public VectorEditingMode EditingMode
    {
        get => GetValue(EditingModeProperty);
        set => SetValue(EditingModeProperty, value);
    }
}
