// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Templates;
using Stride.Assets.Templates;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// The Avalonia <see cref="IAddLibraryParameterPrompt"/>: shows <see cref="ProjectLibraryWindow"/> modally.
/// </summary>
internal sealed class AvaloniaAddLibraryParameterPrompt : IAddLibraryParameterPrompt
{
    public async Task<AddLibraryResult?> PromptAsync(string defaultName, Func<string, bool> isNameTaken)
    {
        var viewModel = new ProjectLibraryViewModel(defaultName, isNameTaken);
        var accepted = await TemplateDialogs.ShowModalAsync(new ProjectLibraryWindow { DataContext = viewModel });
        return accepted ? new AddLibraryResult(viewModel.LibraryName, viewModel.Namespace) : null;
    }
}
