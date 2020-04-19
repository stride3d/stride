// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    /// <summary>
    /// Common view model for items that will display in the property grid for <see cref="GraphicsCompositorEditorViewModel"/>.
    /// </summary>
    public abstract class GraphicsCompositorItemViewModel : DispatcherViewModel, IAssetPropertyProviderViewModel
    {
        protected GraphicsCompositorItemViewModel([NotNull] GraphicsCompositorEditorViewModel editor) : base(editor.SafeArgument(nameof(editor)).ServiceProvider)
        {
            Editor = editor;
            Id = new AbsoluteId(editor.Asset.Id, Guid.NewGuid());
        }

        public AbsoluteId Id { get; }

        public GraphicsCompositorEditorViewModel Editor { get; }

        #region IPropertyProviderViewModel
        /// <inheritdoc/>
        public bool CanProvidePropertiesViewModel => true;

        /// <inheritdoc/>
        public abstract IObjectNode GetRootNode();

        /// <inheritdoc/>
        public virtual bool ShouldConstructMember(IMemberNode member) => ((IPropertyProviderViewModel)Editor.Asset).ShouldConstructMember(member);

        /// <inheritdoc/>
        public bool ShouldConstructItem(IObjectNode collection, NodeIndex index) => ((IPropertyProviderViewModel)Editor.Asset).ShouldConstructItem(collection, index);

        #endregion

        /// <summary>
        /// Gets the path to this item in the asset.
        /// </summary>
        /// <remarks>In case of a virtual node, this method should return an equivalent path if possible; otherwise the path the the closest non-virtual ancestor item.</remarks>
        /// <seealso cref="IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode"/>>
        [NotNull]
        protected abstract GraphNodePath GetNodePath();

        /// <inheritdoc />
        AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Editor.Asset;

        /// <inheritdoc />
        GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
        {
            return GetNodePath();
        }
    }
}
