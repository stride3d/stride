// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Editor.Preview;
using Stride.Editor.Preview.View;

#nullable enable

namespace Stride.Editor.Annotations;

/// <summary>
/// Annotates a type that implements the view of an asset preview.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[BaseTypeRequired(typeof(IPreviewView))]
public class AssetPreviewViewAttribute : Attribute
{
    /// <param name="assetPreviewType">The preview type, or an open generic preview base such as
    /// <c>PreviewFromSpriteBatch&lt;&gt;</c> to serve every preview deriving from it that has no view of its own.</param>
    public AssetPreviewViewAttribute(Type assetPreviewType)
    {
        AssetPreviewType = assetPreviewType;
    }

    /// <summary>
    /// The asset preview type associated with this attribute.
    /// </summary>
    public Type AssetPreviewType { get; }
}

/// <inheritdoc />
public sealed class AssetPreviewViewAttribute<TAssetPreview> : AssetPreviewViewAttribute
    where TAssetPreview : IAssetPreview
{
    public AssetPreviewViewAttribute()
        : base(typeof(TAssetPreview))
    {
    }
}
