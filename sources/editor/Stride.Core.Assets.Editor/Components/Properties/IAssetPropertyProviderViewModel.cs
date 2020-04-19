// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Components.Properties
{
    public interface IAssetPropertyProviderViewModel : IPropertyProviderViewModel
    {
        [NotNull]
        AssetViewModel RelatedAsset { get; }
        
        /// <summary>
        /// Retrieves the absolute path from the original provider to the root node use to generate properties.
        /// </summary>
        /// <remarks>Can be empty.</remarks>
        [NotNull]
        GraphNodePath GetAbsolutePathToRootNode();
    }
}
