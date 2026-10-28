// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.ViewModel;

#nullable enable

namespace Stride.Core.Assets.Editor.Annotations;

public abstract class AssetViewModelAttribute : Attribute
{
    /// <summary>
    /// The type of the asset.
    /// </summary>
    /// <seealso cref="Asset"/>
    public abstract Type AssetType { get; }
}

/// <summary>
/// This attribute is used to register a view model class and associate it to an asset type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(AssetViewModel))]
public sealed class AssetViewModelAttribute<T> : AssetViewModelAttribute
    where T : Asset
{
    /// <inheritdoc />
    public override Type AssetType => typeof(T);
}
