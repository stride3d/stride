// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Templates;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// An item of a template list: a template to instantiate, or a project to open (see <see cref="ExistingProjectViewModel"/>).
/// </summary>
/// <remarks>
/// The icon and the screenshots are paths to image files: the view loads them (the WPF view models hold <c>BitmapImage</c>s).
/// </remarks>
public interface ITemplateDescriptionViewModel
{
    string Name { get; }

    string Description { get; }

    string FullDescription { get; }

    string Group { get; }

    Guid Id { get; }

    string DefaultOutputName { get; }

    /// <summary>
    /// The path of the icon of the item, or <c>null</c> for the default icon.
    /// </summary>
    UFile? Icon { get; }

    /// <summary>
    /// The paths of the screenshots of the item, which exist on disk.
    /// </summary>
    IReadOnlyList<UFile> Screenshots { get; }

    /// <summary>
    /// The template this item instantiates, or <c>null</c> when it is not a template.
    /// </summary>
    TemplateDescription? GetTemplate();
}
