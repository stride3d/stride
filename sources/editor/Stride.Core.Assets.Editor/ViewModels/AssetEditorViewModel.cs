// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// An interface that represents the view model of an asset editor.
/// </summary>
/// <typeparam name="TAsset"></typeparam>
public interface IAssetEditorViewModel<out TAsset>
    where TAsset : AssetViewModel
{
    /// <summary>
    /// The asset related to this editor.
    /// </summary>
    TAsset Asset { get; }
}

/// <summary>
/// Base view model for asset editors.
/// </summary>
/// <typeparam name="TAsset"></typeparam>
public class AssetEditorViewModel<TAsset> : AssetEditorViewModel, IAssetEditorViewModel<TAsset>
    where TAsset : AssetViewModel
{
    public AssetEditorViewModel(TAsset asset)
        : base(asset)
    {
    }

    /// <inheritdoc cref="IAssetEditorViewModel{T}.Asset" />
    public override TAsset Asset => (TAsset)base.Asset;

    public SessionObjectPropertiesViewModel EditorProperties => Session.ActiveProperties;
}

/// <summary>
/// Base view model for asset editors. It implements the toolkit-neutral <see cref="IAssetEditorViewModel"/> of the plugin
/// contracts, so the plugins register the editors and their views the same way for every editor shell.
/// </summary>
public abstract class AssetEditorViewModel : DispatcherViewModel, IAssetEditorViewModel
{
    protected AssetEditorViewModel(AssetViewModel asset)
        : base(asset.ServiceProvider)
    {
        Asset = asset;
    }

    /// <summary>
    /// The asset related to this editor.
    /// </summary>
    public virtual AssetViewModel Asset { get; }

    /// <summary>
    /// The current session.
    /// </summary>
    public SessionViewModel Session => (SessionViewModel)Asset.Session;

    /// <summary>
    /// A task that completes once the editor is initialized; the content loader of an editor game waits for it before
    /// reloading changed assets. Editors with an asynchronous initialization override it.
    /// </summary>
    public virtual Task EditorInitialized => Task.CompletedTask;

    /// <summary>
    /// Initializes this editor asynchronously. The default initialization has nothing to do and succeeds.
    /// </summary>
    /// <returns>A task that completes when the initialization has ended, with <c>true</c> when it succeeded.</returns>
    public virtual Task<bool> Initialize()
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Processes anything before closing the editor, such as saving the file. The default editor closes without a question.
    /// </summary>
    /// <param name="save"><c>true</c> if the file should be saved, <c>false</c> if it can be closed without asking the user, <c>null</c> if the user should be asked.</param>
    /// <returns><c>true</c> if the editor can be closed, <c>false</c> if the user interrupted.</returns>
    public virtual bool PreviewClose(bool? save)
    {
        return true;
    }

    /// <summary>
    /// Processes anything before closing the editor, such as saving the file, and asks the user when needed. The editor
    /// collection calls this one, since a question to the user is asynchronous. The default calls <see cref="PreviewClose"/>.
    /// </summary>
    /// <param name="save"><c>true</c> if the file should be saved, <c>false</c> if it can be closed without asking the user, <c>null</c> if the user should be asked.</param>
    /// <returns>A task that completes with <c>true</c> if the editor can be closed, <c>false</c> if the user interrupted.</returns>
    public virtual Task<bool> PreviewCloseAsync(bool? save)
    {
        return Task.FromResult(PreviewClose(save));
    }
}
