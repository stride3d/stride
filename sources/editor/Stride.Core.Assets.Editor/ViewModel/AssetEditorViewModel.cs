// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// Base view model for asset editors.
    /// </summary>
    public abstract class AssetEditorViewModel : DispatcherViewModel, IAssetEditorViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        protected AssetEditorViewModel([NotNull] AssetViewModel asset)
            : base(asset.SafeArgument(nameof(asset)).ServiceProvider)
        {
            Asset = asset;
        }

        /// <summary>
        /// The undo/redo service used by this view model.
        /// </summary>
        public IUndoRedoService UndoRedoService => ServiceProvider.Get<IUndoRedoService>();

        /// <inheritdoc/>
        [NotNull]
        public SessionObjectPropertiesViewModel EditorProperties => Asset.Session.AssetViewProperties;

        /// <summary>
        /// The current session.
        /// </summary>
        [NotNull]
        public SessionViewModel Session => Asset.Session;

        /// <summary>
        /// The asset related to this editor.
        /// </summary>
        protected AssetViewModel Asset { get; }

        /// <inheritdoc/>
        AssetViewModel IAssetEditorViewModel.Asset => Asset;

        /// <inheritdoc/>
        public abstract Task<bool> Initialize();

        /// <inheritdoc/>
        public virtual bool PreviewClose(bool? save)
        {
            return true;
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
}
