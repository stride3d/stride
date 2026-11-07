// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Templates;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Components.AddAssets;

/// <summary>
/// The asset templates that can import a set of files (see <see cref="ViewModels.AssetCollectionViewModel"/> adding files), for the
/// user to pick one, with the number of files each one accepts when the files are of several kinds.
/// </summary>
public sealed class AssetTemplatesViewModel : DispatcherViewModel
{
    private AssetTemplateItemViewModel? selectedTemplate;

    /// <param name="serviceProvider">The services of the view model.</param>
    /// <param name="templates">The templates, every one accepting every file.</param>
    public AssetTemplatesViewModel(IViewModelServiceProvider serviceProvider, IEnumerable<TemplateAssetDescription> templates)
        : this(serviceProvider, 0, templates.Select(x => new KeyValuePair<TemplateAssetDescription, int>(x, 0)))
    {
    }

    /// <param name="serviceProvider">The services of the view model.</param>
    /// <param name="fileCount">The number of files to import.</param>
    /// <param name="templates">The templates with the number of files each one accepts.</param>
    public AssetTemplatesViewModel(IViewModelServiceProvider serviceProvider, int fileCount, IEnumerable<KeyValuePair<TemplateAssetDescription, int>> templates)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(templates);
        FileCount = fileCount;
        var list = templates.Select(x => new AssetTemplateItemViewModel(new TemplateDescriptionViewModel(serviceProvider, x.Key), x.Value, fileCount)).ToList();
        list.Sort((x, y) => x.Template.Order.CompareTo(y.Template.Order));
        Templates = list;
    }

    /// <summary>
    /// The templates by order, each with the number of files it accepts (0 when every template accepts every file).
    /// </summary>
    public IReadOnlyList<AssetTemplateItemViewModel> Templates { get; }

    /// <summary>
    /// The number of files to import; 0 when every template accepts every file, which hides the counts.
    /// </summary>
    public int FileCount { get; }

    public AssetTemplateItemViewModel? SelectedTemplate
    {
        get => selectedTemplate;
        set => SetValue(ref selectedTemplate, value);
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        foreach (var template in Templates)
        {
            template.Template.Destroy();
        }
        base.Destroy();
    }
}

/// <summary>
/// A template of <see cref="AssetTemplatesViewModel"/> with the number of files it accepts.
/// </summary>
public sealed class AssetTemplateItemViewModel
{
    internal AssetTemplateItemViewModel(TemplateDescriptionViewModel template, int fileCount, int totalFileCount)
    {
        Template = template;
        FileCount = fileCount;
        CountText = totalFileCount > 0 ? string.Format(Tr._p("Label", "{0} out of {1} file(s)"), fileCount, totalFileCount) : string.Empty;
    }

    public TemplateDescriptionViewModel Template { get; }

    /// <summary>
    /// The number of files the template accepts; 0 when every template accepts every file.
    /// </summary>
    public int FileCount { get; }

    /// <summary>
    /// "N out of M file(s)", or empty when every template accepts every file.
    /// </summary>
    public string CountText { get; }

    /// <inheritdoc/>
    public override string ToString() => Template.Name;
}
