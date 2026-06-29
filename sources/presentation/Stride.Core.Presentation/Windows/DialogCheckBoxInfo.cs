// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Presentation.Windows;

/// <summary>
/// Represents a check box in a checkable message box. The same instance carries the initial state in and
/// the user's choice out: <see cref="IsChecked"/> reflects the user's selection once the dialog closes.
/// </summary>
public sealed class DialogCheckBoxInfo : ViewModelBase
{
    private object? content;
    private bool? isChecked;

    /// <summary>
    /// The label shown next to the check box.
    /// </summary>
    public object? Content
    {
        get => content;
        set => SetValue(ref content, value);
    }

    /// <summary>
    /// The check state: the initial value going in, the user's choice coming out.
    /// </summary>
    public bool? IsChecked
    {
        get => isChecked;
        set => SetValue(ref isChecked, value);
    }
}
