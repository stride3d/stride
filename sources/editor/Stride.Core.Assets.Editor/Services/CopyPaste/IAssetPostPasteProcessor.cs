// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// Interface for an asset post-paste processor used by the <see cref="ICopyPasteService"/>.
    /// </summary>
    public interface IAssetPostPasteProcessor
    {
        /// <summary>
        /// Gets whether this processor is able to process asset of the provided <paramref name="assetType"/>.
        /// </summary>
        /// <param name="assetType">The type of asset to process.</param>
        /// <returns><c>true</c> if this processor is able to process the asset; otherwise, <c>false</c>.</returns>
        bool Accept(Type assetType);

        /// <summary>
        /// Applies a post-paste processing to the asset.
        /// </summary>
        /// <param name="asset">The asset to process.</param>
        void PostPasteDeserialization([NotNull] Asset asset);
    }
}
