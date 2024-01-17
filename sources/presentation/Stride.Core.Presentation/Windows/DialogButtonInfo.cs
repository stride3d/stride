// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Presentation.Windows;

/// <summary>
/// Represents a button in a dialog window.
/// </summary>
public sealed class DialogButtonInfo : ViewModelBase
{
    private object? content;
    private bool isCancel;
    private bool isDefault;
    private string key = string.Empty;
    private int result;

    /// <summary>
    /// The content of this button.
    /// </summary>
    public object? Content
    {
        get => content;
        set => SetValue(ref content, value);
    }

    /// <summary>
    /// Specifies whether or not this button is the cancel button.
    /// </summary>
    public bool IsCancel
    {
        get => isCancel;
        set => SetValue(ref isCancel, value);
    }

    /// <summary>
    /// Specifies whether or not this button is the default button.
    /// </summary>
    public bool IsDefault
    {
        get => isDefault;
        set => SetValue(ref isDefault, value);
    }

    /// <summary>
    /// The gesture associated with this button.
    /// </summary>
    public string Key
    {
        get => key;
        set => SetValue(ref key, value);
    }

    /// <summary>
    /// The result associated with this button.
    /// </summary>
    /// <seealso cref="MessageDialogBase.ButtonResult"/>
    public int Result
    {
        get => result;
        set => SetValue(ref result, value);
    }
}
