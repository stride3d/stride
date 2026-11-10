// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// One choice of a <see cref="MultiChoiceTemplateParameterViewModel"/>, shown as a check box.
/// </summary>
public sealed class TemplateChoiceViewModel : ViewModelBase
{
    private bool isChecked;

    /// <param name="key">The choice value, as the template declares it.</param>
    /// <param name="label">The text shown to the user.</param>
    /// <param name="isChecked">Whether the choice is part of the parameter's default value.</param>
    /// <param name="platform">The platform the choice stands for (its icon), for the platforms parameter.</param>
    public TemplateChoiceViewModel(string key, string label, bool isChecked, PlatformType? platform = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(label);
        Key = key;
        Label = label;
        this.isChecked = isChecked;
        Platform = platform;
    }

    public string Key { get; }

    public string Label { get; }

    /// <summary>
    /// The platform this choice stands for, when the parameter lists platforms; null otherwise.
    /// </summary>
    public PlatformType? Platform { get; }

    public bool IsChecked
    {
        get => isChecked;
        set => SetValue(ref isChecked, value);
    }
}
