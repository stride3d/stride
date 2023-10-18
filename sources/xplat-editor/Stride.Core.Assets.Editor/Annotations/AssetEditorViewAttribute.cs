// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Editors;

namespace Stride.Core.Assets.Editor.Annotations;

public abstract class AssetEditorViewAttribute : Attribute
{
    public abstract Type AssetType { get; }
}

/// <summary>
/// This attribute is used to register an editor view class and associate it to an asset type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(IAssetEditorView))]
public sealed class AssetEditorViewAttribute<T> : AssetEditorViewAttribute
{
    /// <summary>
    /// The asset type described by this attribute.
    /// </summary>
    public override Type AssetType => typeof(T);
}
