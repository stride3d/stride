// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Templates;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// The Avalonia <see cref="IModelImportPrompt"/>: shows <see cref="ModelAssetTemplateWindow"/> modally.
/// </summary>
internal sealed class AvaloniaModelImportPrompt : IModelImportPrompt
{
    public Task<bool> PromptAsync(ImportModelFromFileViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        return TemplateDialogs.ShowModalAsync(new ModelAssetTemplateWindow { DataContext = viewModel });
    }
}
