// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Templates;
using Stride.Assets.Templates;
using Stride.Core.Translation;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// The Avalonia <see cref="IAddPluginParameterPrompt"/>: shows <see cref="ProjectLibraryWindow"/> modally, without the namespace
/// (the plugin's namespace is its name, as the template's projects are named after it).
/// </summary>
internal sealed class AvaloniaAddPluginParameterPrompt : IAddPluginParameterPrompt
{
    public async Task<AddPluginResult?> PromptAsync(string defaultName, Func<string, bool> isNameTaken, IReadOnlyList<string> referencingProjects)
    {
        var viewModel = new ProjectLibraryViewModel(defaultName, isNameTaken, Tr._p("Title", "New plugin"), referencingProjects, showNamespace: false, nameLabel: Tr._p("Label", "Plugin name:"));
        var accepted = await TemplateDialogs.ShowModalAsync(new ProjectLibraryWindow { DataContext = viewModel });
        return accepted ? new AddPluginResult(viewModel.LibraryName, viewModel.SelectedReferencingProject) : null;
    }
}
