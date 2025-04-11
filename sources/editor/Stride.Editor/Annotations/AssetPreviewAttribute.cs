// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Annotations;
using Stride.Editor.Preview;

namespace Stride.Editor.Annotations;

/// <summary>
/// Annotates a type that implements an asset preview.
/// </summary>
public abstract class AssetPreviewAttribute : Attribute
{
    /// <summary>
    /// The asset type described by this attribute.
    /// </summary>
    public abstract Type AssetType { get; }
}

/// <inheritdoc />
[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(IAssetPreview))]
public sealed class AssetPreviewAttribute<TAsset> : AssetPreviewAttribute
    where TAsset : Asset
{
    /// <inheritdoc />
    public override Type AssetType => typeof(TAsset);
}
