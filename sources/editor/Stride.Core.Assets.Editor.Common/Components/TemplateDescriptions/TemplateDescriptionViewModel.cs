// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Templates;
using Stride.Core.IO;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// A template of the template manager, as listed by the new project windows.
/// </summary>
public class TemplateDescriptionViewModel : DispatcherViewModel, ITemplateDescriptionViewModel
{
    protected readonly TemplateDescription Template;

    public TemplateDescriptionViewModel(IViewModelServiceProvider serviceProvider, TemplateDescription template)
        : base(serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(template);
        Template = template;
        // The paths of the template are relative to its file; an image that does not exist falls back to the default icon
        Icon = !string.IsNullOrEmpty(template.Icon) ? ExistingImage(GetPath(template.Icon)) : null;
        Screenshots = template.Screenshots.Select(GetPath).Select(ExistingImage).OfType<UFile>().ToList();
    }

    public string Name => Template.Name;

    public string Description => Template.Description;

    public string FullDescription => Template.FullDescription;

    public string Group => Template.Group;

    public int Order => Template.Order;

    public Guid Id => Template.Id;

    public string DefaultOutputName => Template.DefaultOutputName;

    /// <inheritdoc/>
    public UFile? Icon { get; }

    /// <inheritdoc/>
    public IReadOnlyList<UFile> Screenshots { get; }

    public TemplateDescription GetTemplate()
    {
        return Template;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{nameof(TemplateDescriptionViewModel)} [{Name}]";
    }

    private UFile GetPath(UFile imagePath)
    {
        return Template.FullPath is not null ? UPath.Combine(Template.FullPath.GetFullDirectory(), imagePath) : imagePath;
    }

    private static UFile? ExistingImage(UFile path)
    {
        try
        {
            return File.Exists(path.ToOSPath()) ? path : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
