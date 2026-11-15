// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.TemplateEngine.Abstractions;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// One asset pack (an item template of the AssetPacks package) offered by the parameters dialog of a game template. None is
/// checked by default.
/// </summary>
public sealed class AssetPackViewModel : ViewModelBase
{
    private bool isChecked;

    public AssetPackViewModel(ITemplateInfo template)
    {
        ArgumentNullException.ThrowIfNull(template);
        Identity = template.Identity;
        Name = template.Name;
        Description = template.Description;
        Label = string.IsNullOrEmpty(template.Description) ? template.Name : $"{template.Name} — {template.Description}";
    }

    /// <summary>
    /// The identity of the pack's template, passed to the generator when the pack is checked.
    /// </summary>
    public string Identity { get; }

    public string Name { get; }

    public string? Description { get; }

    public string Label { get; }

    public bool IsChecked
    {
        get => isChecked;
        set => SetValue(ref isChecked, value);
    }
}
