// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;

namespace Xenko.Editor.Preview.ViewModel
{
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(IAssetPreviewViewModel))]
    public sealed class AssetPreviewViewModelAttribute : Attribute
    {
        public AssetPreviewViewModelAttribute(Type assetPreviewType)
        {
            if (assetPreviewType == null) throw new ArgumentNullException(nameof(assetPreviewType));

            AssetPreviewType = assetPreviewType;
        }

        public Type AssetPreviewType { get; set; }
    }
}
