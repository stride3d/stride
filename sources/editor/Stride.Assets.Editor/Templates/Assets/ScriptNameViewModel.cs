// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Templates;
using Stride.Core.Extensions;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;
using Stride.Engine;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// The class name, the namespace and the template asked for a new script (see <see cref="ScriptTemplateGenerator"/>).
/// <see cref="Validate"/> turns the names into valid C# identifiers when the user accepts.
/// </summary>
public sealed class ScriptNameViewModel : ViewModelBase
{
    private static readonly List<string> ReservedNames = [];

    private string className;
    private string ns;
    private TemplateAssetDescription? selectedTemplate;

    static ScriptNameViewModel()
    {
        // Reserve the name of the base classes and interfaces
        ReservedNames.Add(nameof(AsyncScript));
        ReservedNames.Add(nameof(SyncScript));
        ReservedNames.Add(nameof(StartupScript));
        typeof(ScriptComponent).GetInterfaces().ForEach(x => ReservedNames.Add(x.Name));
        var type = typeof(ScriptComponent);
        while (type is not null && type != typeof(object))
        {
            ReservedNames.Add(type.Name);
            type = type.BaseType;
        }
    }

    /// <param name="defaultClassName">The proposed class name.</param>
    /// <param name="defaultNamespace">The proposed namespace.</param>
    /// <param name="defaultTemplate">The template the user picked, proposed in the list.</param>
    /// <param name="enableTemplateSelect">Whether the user can pick another script template.</param>
    /// <param name="templates">The script templates to pick from when <paramref name="enableTemplateSelect"/>.</param>
    public ScriptNameViewModel(string defaultClassName, string? defaultNamespace, TemplateAssetDescription defaultTemplate, bool enableTemplateSelect = false, IEnumerable<TemplateAssetDescription>? templates = null)
    {
        ArgumentNullException.ThrowIfNull(defaultClassName);
        ArgumentNullException.ThrowIfNull(defaultTemplate);
        className = defaultClassName;
        ns = defaultNamespace ?? string.Empty;
        DefaultTemplate = defaultTemplate;
        EnableTemplateSelect = enableTemplateSelect;
        Templates = enableTemplateSelect ? (templates ?? []).ToList() : [defaultTemplate];
        selectedTemplate = Templates.Contains(defaultTemplate) ? defaultTemplate : Templates.FirstOrDefault();
    }

    public string Title => Tr._p("Title", "New script");

    /// <summary>
    /// The class name as typed; a valid identifier after <see cref="Validate"/>.
    /// </summary>
    public string ClassName
    {
        get => className;
        set => SetValue(ref className, value ?? string.Empty);
    }

    /// <summary>
    /// The namespace as typed; a valid namespace after <see cref="Validate"/>.
    /// </summary>
    public string Namespace
    {
        get => ns;
        set => SetValue(ref ns, value ?? string.Empty);
    }

    /// <summary>
    /// The template the user picked before the dialog.
    /// </summary>
    public TemplateAssetDescription DefaultTemplate { get; }

    /// <summary>
    /// Whether the user can pick another script template.
    /// </summary>
    public bool EnableTemplateSelect { get; }

    /// <summary>
    /// The script templates to pick from; the default one alone when the choice is disabled.
    /// </summary>
    public IReadOnlyList<TemplateAssetDescription> Templates { get; }

    /// <summary>
    /// The script template of the new script.
    /// </summary>
    public TemplateAssetDescription? SelectedTemplate
    {
        get => selectedTemplate;
        set => SetValue(ref selectedTemplate, value);
    }

    /// <summary>
    /// The template the script is created from: the selected one, or the default one when the choice is disabled.
    /// </summary>
    public TemplateAssetDescription ScriptTemplate => (EnableTemplateSelect ? SelectedTemplate : null) ?? DefaultTemplate;

    /// <summary>
    /// Turns the names into valid C# identifiers (the reserved script class names are suffixed) and checks they are not empty.
    /// </summary>
    /// <param name="error">The error to show when the names are not valid.</param>
    /// <returns><c>true</c> when the script can be created.</returns>
    public bool Validate(out string error)
    {
        error = Tr._p("Message", "The names you entered are invalid or empty.");
        // The builders need a character to start from
        if (string.IsNullOrWhiteSpace(ClassName) || string.IsNullOrWhiteSpace(Namespace))
            return false;

        ClassName = Utilities.BuildValidClassName(ClassName.Trim(), ReservedNames);
        Namespace = Utilities.BuildValidNamespaceName(Namespace.Trim(), ReservedNames);
        if (string.IsNullOrWhiteSpace(ClassName) || string.IsNullOrWhiteSpace(Namespace))
            return false;

        error = string.Empty;
        return true;
    }
}
