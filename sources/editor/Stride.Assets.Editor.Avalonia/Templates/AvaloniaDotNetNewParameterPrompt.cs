// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.TemplateEngine.Abstractions;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Templates;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// The Avalonia <see cref="IDotNetNewParameterPrompt"/>: shows <see cref="DotNetNewTemplateParametersWindow"/> modally.
/// </summary>
internal sealed class AvaloniaDotNetNewParameterPrompt : IDotNetNewParameterPrompt
{
    public async Task<DotNetNewPromptResult?> PromptAsync(ITemplateInfo template, TemplateDotNetNewDescription description)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(description);

        // The templates that offer asset packs get the pack check boxes; the bridge downloads the AssetPacks package on first use,
        // while the window is open. An empty list (offline) means no packs.
        var assetPacks = description.OffersAssetPacks ? DotNetNewTemplateBridge.GetAssetPackTemplatesAsync() : null;

        var serviceProvider = new ViewModelServiceProvider([DispatcherService.Create()]);
        var viewModel = new DotNetNewTemplateParametersViewModel(serviceProvider, template, assetPacks);
        try
        {
            var accepted = await TemplateDialogs.ShowModalAsync(new DotNetNewTemplateParametersWindow { DataContext = viewModel });
            return accepted ? viewModel.GetResult() : null;
        }
        finally
        {
            viewModel.Destroy();
        }
    }
}
