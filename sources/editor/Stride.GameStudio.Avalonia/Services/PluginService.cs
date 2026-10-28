// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Media.Imaging;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Views;
using Stride.Editor;

namespace Stride.GameStudio.Avalonia.Services;

/// <summary>
/// Registers the plugins of all asset assemblies with the session, including assemblies loaded after it opened, and
/// removes what a plugin registered when its assembly is unloaded. The Avalonia side of the <see cref="EditorPluginHost"/>
/// shared with the WPF Game Studio: the template providers go to the property grid's <see cref="PropertyViewHelper"/>
/// selectors, the editor views are <see cref="IAssetEditorView"/>s and the declared images Avalonia bitmaps.
/// </summary>
/// <remarks>
/// Disposing it (the editor closes its session) takes back what the plugins registered, including the template providers
/// of the property grid, which are process-wide: the plugins join the next editor's service afresh.
/// </remarks>
internal sealed class PluginService : StrideEditorPluginHost
{
    private readonly IDispatcherService dispatcherService;

    public PluginService(IDispatcherService dispatcherService)
        : base(dispatcher: dispatcherService)
    {
        this.dispatcherService = dispatcherService;
    }

    /// <inheritdoc />
    protected override Type AssetEditorViewModelType => typeof(AssetEditorViewModel);

    /// <inheritdoc />
    protected override Type AssetEditorViewType => typeof(IAssetEditorView);

    /// <inheritdoc />
    /// <remarks>Only the Avalonia providers (<see cref="TemplateProviderBase"/>) are taken.</remarks>
    protected override bool AddTemplateProvider(ISessionViewModel session, ITemplateProvider provider)
    {
        if (provider is not TemplateProviderBase avaloniaProvider)
            return false;

        dispatcherService.Invoke(() => GetSelector(avaloniaProvider).RegisterTemplateProvider(avaloniaProvider));
        return true;
    }

    /// <inheritdoc />
    protected override void RemoveTemplateProvider(ISessionViewModel session, ITemplateProvider provider)
    {
        var avaloniaProvider = (TemplateProviderBase)provider;
        dispatcherService.Invoke(() => GetSelector(avaloniaProvider).UnregisterTemplateProvider(avaloniaProvider));
    }

    /// <inheritdoc />
    protected override object DecodeImage(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return new Bitmap(stream);
    }

    /// <inheritdoc />
    protected override bool IsEditorInitialized(ISessionViewModel session) => session is SessionViewModel { IsEditorInitialized: true };

    // The property grid selector a provider belongs to, by its template category
    private static TemplateProviderSelector GetSelector(TemplateProviderBase provider)
    {
        return PropertyViewHelper.GetTemplateCategory(provider) switch
        {
            PropertyViewHelper.Category.PropertyHeader => PropertyViewHelper.HeaderProviders,
            PropertyViewHelper.Category.PropertyFooter => PropertyViewHelper.FooterProviders,
            PropertyViewHelper.Category.PropertyEditor => PropertyViewHelper.EditorProviders,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), "Unknown template category"),
        };
    }
}
