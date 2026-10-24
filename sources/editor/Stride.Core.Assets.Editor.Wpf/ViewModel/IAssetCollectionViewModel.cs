// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// The part of an asset view (<see cref="AssetCollectionViewModel"/>) that the session objects use.
    /// </summary>
    public interface IAssetCollectionViewModel
    {
        /// <summary>
        /// Gets the assets that are currently selected.
        /// </summary>
        IReadOnlyObservableCollection<AssetViewModel> SelectedAssets { get; }
    }
}
