// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Threading;
using Stride.Assets.Editor.Templates;
using Stride.Assets.Templates;
using Stride.Core;
using Stride.Core.Assets;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// The Avalonia <see cref="IUpdatePlatformsParameterPrompt"/>: shows <see cref="UpdatePlatformsWindow"/> modally, with the supported
/// platforms, as the WPF prompt does.
/// </summary>
internal sealed class AvaloniaUpdatePlatformsParameterPrompt : IUpdatePlatformsParameterPrompt
{
    public async Task<UpdatePlatformsResult?> PromptAsync(ISet<PlatformType> installed)
    {
        ArgumentNullException.ThrowIfNull(installed);
        var viewModel = new UpdatePlatformsViewModel(AssetRegistry.SupportedPlatforms, installed);
        var window = await Dispatcher.UIThread.InvokeAsync(() => new UpdatePlatformsWindow { DataContext = viewModel });
        var accepted = await TemplateDialogs.ShowModalAsync(window);
        return accepted ? viewModel.ToResult() : null;
    }
}
