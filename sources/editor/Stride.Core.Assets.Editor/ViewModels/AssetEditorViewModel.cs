// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

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

    /// <summary>
    /// Initializes an editor that uses the services of its tab (see <see cref="AssetEditorViewModel(AssetViewModel, IViewModelServiceProvider)"/>).
    /// </summary>
    [Experimental("STREXP006")]
    public AssetEditorViewModel(TAsset asset, IViewModelServiceProvider serviceProvider)
        : base(asset, serviceProvider)
    {
    }

    /// <inheritdoc cref="IAssetEditorViewModel{T}.Asset" />
    public override TAsset Asset => (TAsset)base.Asset;
}

/// <summary>
/// Base view model for asset editors. It implements the toolkit-neutral <see cref="IAssetEditorViewModel"/> of the plugin
/// contracts, so the plugins register the editors and their views the same way for every editor shell.
/// </summary>
public abstract class AssetEditorViewModel : DispatcherViewModel, IAssetEditorViewModel
{
    // The services of the tab of this editor, which it disposes when it is destroyed
    private ViewModelServiceProvider? tabServices;

    /// <summary>
    /// Initializes an editor that uses the services of the session of its asset.
    /// </summary>
    protected AssetEditorViewModel(AssetViewModel asset)
        : base(asset.ServiceProvider)
    {
        Asset = asset;
    }

    /// <summary>
    /// Initializes an editor that uses the services of its tab: a child of the session's services, made for this editor by
    /// <see cref="Create"/>, where the editor can register services of its own. A constructor that asks for an
    /// <see cref="IViewModelServiceProvider"/> gets them.
    /// </summary>
    [Experimental("STREXP006")]
    protected AssetEditorViewModel(AssetViewModel asset, IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        Asset = asset;
    }

    /// <summary>
    /// Creates the editor of <paramref name="asset"/>, with a scope of services for its tab (a child of the session's services)
    /// that goes when the editor is destroyed. The constructor gets the asset, and any service it asks for: the longest
    /// constructor that can be called is used, and the tab's services are what a parameter of type
    /// <see cref="IViewModelServiceProvider"/> or <see cref="IServiceProvider"/> gets.
    /// </summary>
    /// <param name="editorType">The type of the editor.</param>
    /// <param name="asset">The asset to edit.</param>
    /// <returns>The new editor.</returns>
    [Experimental("STREXP006")]
    public static AssetEditorViewModel Create(Type editorType, AssetViewModel asset)
    {
        ArgumentNullException.ThrowIfNull(editorType);
        ArgumentNullException.ThrowIfNull(asset);

        var services = new ViewModelServiceProvider(asset.ServiceProvider);
        try
        {
            var editor = (AssetEditorViewModel)services.CreateInstance(editorType, asset);
            editor.tabServices = services;
            return editor;
        }
        catch
        {
            services.Dispose();
            throw;
        }
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        base.Destroy();
        tabServices?.Dispose();
        tabServices = null;
    }

    /// <summary>
    /// The asset related to this editor.
    /// </summary>
    public virtual AssetViewModel Asset { get; }

    /// <summary>
    /// The current session.
    /// </summary>
    /// <remarks>Asset editors only exist in the editor's session, so the session of their asset is always a <see cref="SessionViewModel"/>.</remarks>
    public SessionViewModel Session => (SessionViewModel)Asset.Session;

    /// <summary>
    /// The undo/redo service used by this view model.
    /// </summary>
    public IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();

    /// <summary>
    /// The properties that the editor shows in the property grid: those of the main asset view of the session
    /// (<see cref="SessionViewModel.AssetViewProperties"/>), which become the active properties when they change.
    /// </summary>
    public SessionObjectPropertiesViewModel EditorProperties => Session.AssetViewProperties;

    /// <summary>
    /// A task that completes once the editor is initialized; the content loader of an editor game waits for it before
    /// reloading changed assets. Editors with an asynchronous initialization override it.
    /// </summary>
    public virtual Task EditorInitialized => Task.CompletedTask;

    /// <summary>
    /// A task that completes once the editor, after <see cref="ViewModelBase.Destroy"/>, runs nothing more on other threads, such as
    /// the frames of the game of a scene editor. The session waits for it before the plugins leave and take their settings keys
    /// with them, which those threads read. Editors that stop asynchronously override it.
    /// </summary>
    public virtual Task Stopped => Task.CompletedTask;

    /// <summary>
    /// Initializes this editor asynchronously. The default initialization has nothing to do and succeeds.
    /// </summary>
    /// <returns>A task that completes when the initialization has ended, with <c>true</c> when it succeeded.</returns>
    public virtual Task<bool> Initialize()
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Saves the changes that the editor keeps outside its asset, such as the text of a script, which its editor writes to the
    /// file itself. The session calls it before it saves; the default editor keeps no such change.
    /// </summary>
    /// <param name="logger">The log of the session save, which gets the errors.</param>
    /// <returns>A task that completes when the changes are saved, or when the user chose not to save them.</returns>
    public virtual Task SaveChangesAsync(ILogger logger)
    {
        return Task.CompletedTask;
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

    /// <summary>
    /// Shows the properties of the <see cref="Asset"/>.
    /// </summary>
    protected void ShowAssetProperties()
    {
        EditorProperties.TypeDescription = Asset.TypeDisplayName;
        EditorProperties.Name = Asset.Name;
        EditorProperties.GenerateSelectionPropertiesAsync(Asset.Yield()).Forget();
    }
}
