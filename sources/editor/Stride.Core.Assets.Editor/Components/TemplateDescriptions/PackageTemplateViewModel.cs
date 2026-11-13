// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Templates;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions;

/// <summary>
/// A package template of the "Update package" menu: its command updates the package selected in the session with it.
/// </summary>
public sealed class PackageTemplateViewModel : TemplateDescriptionViewModel
{
    private readonly SessionViewModel? session;

    public PackageTemplateViewModel(IViewModelServiceProvider serviceProvider, TemplateDescription template, SessionViewModel? session = null)
        : base(serviceProvider, template)
    {
        this.session = session;
        UpdatePackageCommand = new AnonymousTaskCommand(ServiceProvider, UpdatePackageAsync) { IsEnabled = session is not null };
    }

    /// <summary>
    /// Updates the package selected in the session with this template.
    /// </summary>
    public ICommandBase UpdatePackageCommand { get; }

    private Task UpdatePackageAsync()
    {
        return session?.UpdatePackageTemplateAsync(Template) ?? Task.CompletedTask;
    }
}
