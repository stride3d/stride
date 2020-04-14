// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Assets;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Editor.Preview.View;

namespace Stride.Editor.Preview
{
    /// <summary>
    /// This attribute is used to register a preview class and associate it to an asset type. A preview view class can also
    /// be specified. If not, then the <see cref="DefaultViewType"/> will be used instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(IAssetPreview))]
    public sealed class AssetPreviewAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPreviewAttribute"/>.
        /// </summary>
        /// <param name="assetType">The asset type. Must be derivated from <see cref="Asset"/>.</param>
        /// <param name="viewType">The preview view type. Must be derivated from <see cref="IPreviewView"/>. If null, the <see cref="DefaultViewType"/> will be used instead.</param>
        public AssetPreviewAttribute(Type assetType, Type viewType = null)
        {
            if (!typeof(Asset).IsAssignableFrom(assetType))
                throw new ArgumentException($"The asset type must inherits from the {nameof(Asset)}.");

            if (viewType != null && !viewType.HasInterface(typeof(IPreviewView)))
                throw new ArgumentException($"The preview view type must implement the {nameof(IPreviewView)} interface.");

            AssetType = assetType;
            ViewType = viewType;
        }

        /// <summary>
        /// Gets or sets the asset type described by this attribute.
        /// </summary>
        public Type AssetType { get; set; }

        /// <summary>
        /// Gets or sets the preview view type associated to the <see cref="AssetType"/>.
        /// </summary>
        public Type ViewType { get; set; }

        /// <summary>
        /// Gets the default view to use when <see cref="ViewType"/> is <c>null</c>.
        /// </summary>
        public static Type DefaultViewType => typeof(StridePreviewView);
    }
}
