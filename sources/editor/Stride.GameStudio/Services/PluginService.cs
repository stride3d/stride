// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Windows.Media.Imaging;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Views;
using Stride.Editor;

namespace Stride.GameStudio.Services;

/// <summary>
/// Registers the plugins of all asset assemblies with the session, including assemblies loaded after it opened. The WPF
/// side of the <see cref="EditorPluginHost"/> shared with the cross-platform editor: the template providers go to the
/// <see cref="IEditorDialogService"/>, the editor views are <see cref="IEditorView"/>s and the declared images WPF images.
/// </summary>
public class PluginService : StrideEditorPluginHost
{
    /// <inheritdoc />
    protected override Type AssetEditorViewModelType => typeof(AssetEditorViewModel);

    /// <inheritdoc />
    protected override Type AssetEditorViewType => typeof(IEditorView);

    /// <inheritdoc />
    protected override bool AddTemplateProvider(ISessionViewModel session, ITemplateProvider provider)
    {
        session.ServiceProvider.Get<IEditorDialogService>().RegisterAdditionalTemplateProvider(provider);
        return true;
    }

    /// <inheritdoc />
    protected override void RemoveTemplateProvider(ISessionViewModel session, ITemplateProvider provider)
    {
        session.ServiceProvider.Get<IEditorDialogService>().UnregisterAdditionalTemplateProvider(provider);
    }

    /// <inheritdoc />
    protected override object DecodeImage(byte[] data)
    {
        // Frozen: decoded on the loader thread, shown on the UI thread
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = new MemoryStream(data);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    /// <inheritdoc />
    protected override bool IsEditorInitialized(ISessionViewModel session) => ((SessionViewModel)session).IsEditorInitialized;
}
