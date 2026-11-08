// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.Components.Properties
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
