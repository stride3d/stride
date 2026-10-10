// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Editor.Preview;
using Stride.Editor.Preview.ViewModel;

#nullable enable

namespace Stride.Editor.Annotations;

/// <summary>
/// Annotates a type that implements the view model of an asset preview.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(IAssetPreviewViewModel))]
public class AssetPreviewViewModelAttribute : Attribute
{
    /// <param name="assetPreviewType">The preview type, or an open generic preview base such as
    /// <c>PreviewFromSpriteBatch&lt;&gt;</c> to serve every preview deriving from it that has no view model of its own.</param>
    public AssetPreviewViewModelAttribute(Type assetPreviewType)
    {
        AssetPreviewType = assetPreviewType;
    }

    /// <summary>
    /// The asset preview type associated with this attribute.
    /// </summary>
    public Type AssetPreviewType { get; }
}

/// <inheritdoc />
public sealed class AssetPreviewViewModelAttribute<TAssetPreview> : AssetPreviewViewModelAttribute
    where TAssetPreview : IAssetPreview
{
    public AssetPreviewViewModelAttribute()
        : base(typeof(TAssetPreview))
    {
    }
}
