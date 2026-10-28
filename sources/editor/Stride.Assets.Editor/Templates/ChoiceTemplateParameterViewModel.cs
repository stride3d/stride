// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.TemplateEngine.Abstractions;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// A single-choice parameter, shown as a combo box of the choice keys.
/// </summary>
public sealed class ChoiceTemplateParameterViewModel : TemplateParameterViewModel
{
    private string? selectedChoice;

    public ChoiceTemplateParameterViewModel(ITemplateParameter parameter)
        : base(parameter)
    {
        Choices = parameter.Choices?.Keys.ToList() ?? [];
        selectedChoice = parameter.DefaultValue is { } defaultValue && Choices.Contains(defaultValue) ? defaultValue : Choices.FirstOrDefault();
    }

    /// <summary>
    /// The keys of the choices, in the template's order.
    /// </summary>
    public IReadOnlyList<string> Choices { get; }

    public string? SelectedChoice
    {
        get => selectedChoice;
        set => SetValue(ref selectedChoice, value);
    }

    /// <inheritdoc/>
    public override string? GetValue() => SelectedChoice;
}
