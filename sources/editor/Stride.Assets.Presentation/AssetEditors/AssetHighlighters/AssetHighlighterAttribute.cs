// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets;

namespace Stride.Assets.Presentation.AssetEditors.AssetHighlighters
{
    /// <summary>
    /// Specifies for which class of asset the associated asset highlighter class is.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AssetHighlighterAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetHighlighterAttribute"/> class.
        /// </summary>
        /// <param name="assetType">The type of asset the related asset highlighter class is associated with.</param>
        public AssetHighlighterAttribute(Type assetType)
        {
            AssetRegistry.IsAssetType(assetType, true);
            AssetType = assetType;
        }

        /// <summary>
        /// Gets or sets the type of asset the related asset highlighter class is associated with.
        /// </summary>
        public Type AssetType { get; set; }
    }
}
