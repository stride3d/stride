// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Annotations;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// An interface that represents a the view model of an editor capable of editing more than one asset.
    /// </summary>
    public interface IMultipleAssetEditorViewModel : IAssetEditorViewModel
    {
        /// <summary>
        /// The list of assets opened in this editor.
        /// </summary>
        [ItemNotNull, NotNull]
        IReadOnlyObservableList<AssetViewModel> OpenedAssets { get; }
    }
}
