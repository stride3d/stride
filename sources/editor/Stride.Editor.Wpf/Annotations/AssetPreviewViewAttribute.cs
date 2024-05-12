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
public abstract class AssetPreviewViewAttribute : Attribute
{
    /// <summary>
    /// The asset preview type associated with this attribute.
    /// </summary>
    public abstract Type AssetPreviewType { get; }
}

/// <inheritdoc />
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[BaseTypeRequired(typeof(IPreviewView))]
public sealed class AssetPreviewViewAttribute<TAssetPreview> : AssetPreviewViewAttribute
    where TAssetPreview : IAssetPreview
{
    /// <inheritdoc />
    public override Type AssetPreviewType => typeof(TAssetPreview);
}
