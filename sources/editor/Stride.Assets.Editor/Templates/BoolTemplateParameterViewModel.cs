// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.TemplateEngine.Abstractions;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// A boolean parameter, shown as a check box.
/// </summary>
public sealed class BoolTemplateParameterViewModel : TemplateParameterViewModel
{
    private bool isChecked;

    public BoolTemplateParameterViewModel(ITemplateParameter parameter)
        : base(parameter)
    {
        isChecked = bool.TryParse(parameter.DefaultValue, out var value) && value;
    }

    public bool IsChecked
    {
        get => isChecked;
        set => SetValue(ref isChecked, value);
    }

    /// <inheritdoc/>
    public override string? GetValue() => IsChecked ? "true" : "false";
}
