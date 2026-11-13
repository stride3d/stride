// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.TemplateEngine.Abstractions;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// A free-form parameter (string, number or an unknown type), shown as a text box. The template engine validates the value when the
/// template is instantiated.
/// </summary>
public sealed class TextTemplateParameterViewModel : TemplateParameterViewModel
{
    private string text;

    public TextTemplateParameterViewModel(ITemplateParameter parameter)
        : base(parameter)
    {
        text = parameter.DefaultValue ?? string.Empty;
    }

    public string Text
    {
        get => text;
        set => SetValue(ref text, value);
    }

    /// <inheritdoc/>
    public override string? GetValue() => string.IsNullOrEmpty(Text) ? null : Text;
}
