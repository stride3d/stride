// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// One user-facing parameter of a dotnet new template in the parameters dialog. The derived types hold the control's value:
/// <see cref="BoolTemplateParameterViewModel"/>, <see cref="ChoiceTemplateParameterViewModel"/>,
/// <see cref="MultiChoiceTemplateParameterViewModel"/> and <see cref="TextTemplateParameterViewModel"/>.
/// </summary>
public abstract class TemplateParameterViewModel : ViewModelBase
{
    private bool isEnabled = true;

    protected TemplateParameterViewModel(ITemplateParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        Parameter = parameter;
        DisplayName = Humanize(parameter.Name);
        Label = string.IsNullOrEmpty(parameter.Description) ? DisplayName : $"{DisplayName} — {parameter.Description}";
    }

    /// <summary>
    /// The parameter of the template.
    /// </summary>
    public ITemplateParameter Parameter { get; }

    /// <summary>
    /// The name of the parameter, as the template declares it (the key of the parameter values).
    /// </summary>
    public string Name => Parameter.Name;

    /// <summary>
    /// The name shown to the user, derived from <see cref="Name"/>.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// The description of the parameter, when the template has one.
    /// </summary>
    public string? Description => Parameter.Description;

    /// <summary>
    /// The label of the parameter's row: its display name and its description.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// The default value declared by the template; a value equal to it is not sent to the template engine.
    /// </summary>
    public string? DefaultValue => Parameter.DefaultValue;

    /// <summary>
    /// Whether the user can change the value. A parameter coupled to another one (HDR to the graphics profile) is disabled when
    /// the other one makes it meaningless.
    /// </summary>
    public bool IsEnabled
    {
        get => isEnabled;
        set => SetValue(ref isEnabled, value);
    }

    /// <summary>
    /// Gets the value to pass to the template engine, in the dotnet new wire format; null for "use the default".
    /// </summary>
    public abstract string? GetValue();

    /// <summary>
    /// Turns a parameter name into a user-friendly label: splits camelCase, kebab-case and snake_case, capitalizes the words and
    /// keeps the acronyms.
    /// </summary>
    public static string Humanize(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c is '-' or '_')
            {
                if (sb.Length > 0 && sb[^1] != ' ')
                    sb.Append(' ');
                continue;
            }
            // A space before an uppercase letter that starts a new word (lower to upper, or an upper followed by a lower in a run of caps)
            if (i > 0 && char.IsUpper(c) && sb.Length > 0 && sb[^1] != ' ')
            {
                var prev = name[i - 1];
                var next = i + 1 < name.Length ? name[i + 1] : '\0';
                if (char.IsLower(prev) || char.IsDigit(prev) || (char.IsUpper(prev) && char.IsLower(next)))
                    sb.Append(' ');
            }
            sb.Append(sb.Length == 0 || sb[^1] == ' ' ? char.ToUpperInvariant(c) : c);
        }
        return sb.ToString();
    }
}
