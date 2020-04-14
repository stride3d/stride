// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Editor.Components.Properties
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
