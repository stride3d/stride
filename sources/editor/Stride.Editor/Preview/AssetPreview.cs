// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Assets;
using Stride.Core.Annotations;
using Stride.Editor.Preview.View;
using Stride.Editor.Preview.ViewModel;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.Core.Presentation.ViewModels;
using Stride.Editor.Annotations;

namespace Stride.Editor.Preview
{
    /// <summary>
    /// A base implementation for the <see cref="IAssetPreview"/>.
    /// </summary>
    public abstract class AssetPreview : IAssetPreview
    {
        private static Dictionary<Type, Type> previewViewModelTypes;
        private static readonly object SyncRoot = new object();
        private TaskCompletionSource<int> initCompletionSource = new TaskCompletionSource<int>();

        protected IPreviewBuilder Builder;
        protected PreviewGame Game;
        protected AssetItem AssetItem;

        private IPreviewView previewView;

        private bool needSceneUpdate;

        /// <summary>
        /// Gets the value indicating if the preview is still running.
        /// </summary>
        protected bool IsRunning { get; private set; }

        /// <inheritdoc/>
        public IAssetPreviewViewModel PreviewViewModel { get; protected set; }

        /// <inheritdoc/>
        public AssetViewModel AssetViewModel { get; private set; }

        public RenderingMode RenderingMode
        {
            get; protected set;
        }

        public static Type DefaultViewType { get; set; }

        /// <summary>
        /// Initializes the preview of an asset. This method will invoke the protected virtual method <see cref="Initialize()"/>.
        /// </summary>
        /// <param name="asset">The view model of the asset to preview.</param>
        /// <param name="builder">The preview builder that is initializing this preview.</param>
        /// <returns>A task returning an object that is the view associated to the preview.</returns>
        public async Task<object> Initialize(AssetViewModel asset, IPreviewBuilder builder)
        {
            IsRunning = true;
            AssetViewModel = asset;
            AssetItem = asset.AssetItem;
            Builder = builder;
            Game = builder.PreviewGame;

            // Copy ColorSpace to Game
            // TODO: Move this code this method and find a better pluggable way to do this.

            var gameSettings = AssetItem.Package.GetGameSettingsAssetOrDefault();
            Game.GraphicsDeviceManager.PreferredColorSpace = DetermineColorSpace();
            Game.GraphicsDeviceManager.ApplyChanges();

            RenderingMode = gameSettings.GetOrCreate<EditorSettings>().RenderingMode;

            await Initialize();
            PreviewViewModel = await ProvideViewModel(asset.ServiceProvider);
            previewView = await ProvideView(asset.ServiceProvider);
            FinalizeInitialization();
            return previewView;
        }

        /// <inheritdoc/>
        public async Task Update()
        {
            await IsInitialized();
            await PrepareContentInternal();
            await Builder.Dispatcher.InvokeAsync(() => previewView.UpdateView(this));
        }

        /// <summary>
        /// Determine the color space to be used by the <see cref="GraphicsDevice"/> when generating the preview.
        /// </summary>
        /// <returns>The color space to use.</returns>
        protected virtual ColorSpace DetermineColorSpace()
        {
            var gameSettings = AssetItem.Package.GetGameSettingsAssetOrDefault();
            var renderingSettings = gameSettings.GetOrCreate<RenderingSettings>();
            return renderingSettings.ColorSpace;
        }

        /// <summary>
        /// Update the scene. Note: this function is called from the PreviewGame thread!
        /// </summary>
        protected RenderingMode UpdateScene()
        {
            if (needSceneUpdate)
            {
                UnloadContent();
                LoadContentSafe();
                Game.TriggerActiveRenderStageReevaluation();
                needSceneUpdate = false;
            }

            return RenderingMode;
        }

        /// <inheritdoc/>
        public async Task IsInitialized()
        {
            await initCompletionSource.Task;
        }

        /// <inheritdoc/>
        public virtual async Task Dispose()
        {
            IsRunning = false;
            initCompletionSource = new TaskCompletionSource<int>();

            // ReSharper disable once DelegateSubtraction
            Game.UpdateSceneCallback -= UpdateScene;

            await Game.UnloadPreviewScene(Builder.Logger);

            UnloadContent();
        }

        /// <summary>
        /// Get or create the preview scene to load into the preview game.
        /// </summary>
        /// <returns>The preview scene</returns>
        protected virtual Scene CreatePreviewScene()
        {
            return null;
        }

        protected virtual GraphicsCompositor GetGraphicsCompositor()
        {
            return null;
        }

        public virtual void OnViewAttached()
        {
        }

        /// <summary>
        /// Initializes the preview of the asset in classes that inherits from <see cref="AssetPreview"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task Initialize()
        {
            await Game.LoadPreviewScene(CreatePreviewScene(), GetGraphicsCompositor(), Builder.Logger);
            Game.UpdateSceneCallback += UpdateScene;

            await PrepareContentInternal();
        }

        private async Task PrepareContentInternal()
        {
            if (await PrepareContent())
                needSceneUpdate = true;
        }

        /// <summary>
        /// Prepare the content used by the preview.
        /// </summary>
        /// <returns><value>True</value> if content could be prepared correctly, <value>False</value> otherwise</returns>
        protected virtual Task<bool> PrepareContent()
        {
            needSceneUpdate = true;

            return Task.FromResult(true);
        }

        private void LoadContentSafe()
        {
            try
            {
                LoadContent();
            }
            catch (Exception e)
            {
                // TODO: In PreviewFromEntity, the preview scene is created during load, leaving the scene in an invalid state when throwing.
                // This can lead to crashes when removing the preview entity (e.g. during Script removal)
                Builder.Logger.Error($"Failed to load the content for the preview of asset '{AssetItem.Location}'.", e);
            }
        }

        /// <summary>
        /// Load the content of the preview
        /// </summary>
        protected virtual void LoadContent()
        {
        }

        /// <summary>
        /// Unload the content of the preview.
        /// </summary>
        protected virtual void UnloadContent()
        {

        }

        private void FinalizeInitialization()
        {
            initCompletionSource.SetResult(0);
        }

        [ItemCanBeNull]
        private async Task<IPreviewView> ProvideView([NotNull] IViewModelServiceProvider serviceProvider)
        {
            var pluginService = serviceProvider.Get<IAssetsPluginService>();
            var viewType = pluginService.GetPreviewViewType(GetType()) ?? DefaultViewType;

            return viewType is not null
                ? await Builder.Dispatcher.InvokeAsync(() =>
                {
                    var view = (IPreviewView)Activator.CreateInstance(viewType);
                    view?.InitializeView(Builder, this);
                    return view;
                })
                : null;
        }

        [ItemCanBeNull]
        private async Task<IAssetPreviewViewModel> ProvideViewModel([NotNull] IViewModelServiceProvider serviceProvider)
        {
            var pluginService = serviceProvider.Get<IAssetsPluginService>();
            var previewViewModelType = pluginService.GetPreviewViewModelType(GetType());

            return previewViewModelType is not null
                ? await AssetViewModel.Dispatcher.InvokeAsync(() =>
                {
                    return (IAssetPreviewViewModel)Activator.CreateInstance(previewViewModelType, AssetViewModel.Session);
                })
                : null;
        }
    }

    /// <summary>
    /// A specialization of the <see cref="AssetPreview"/> class that specifies the related asset type as a generic argument.
    /// </summary>
    /// <typeparam name="T">The type of asset this class manages to preview.</typeparam>
    public abstract class AssetPreview<T> : AssetPreview where T : Asset
    {
        protected T Asset => (T)AssetItem.Asset;
    }
}
