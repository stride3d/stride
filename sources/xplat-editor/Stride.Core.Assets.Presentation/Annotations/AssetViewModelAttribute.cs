// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.Annotations;

public abstract class AssetViewModelAttribute : Attribute
{
    public abstract Type AssetType { get; }
}

/// <summary>
/// This attribute is used to register a view model class and associate it to an asset type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(AssetViewModel))]
public sealed class AssetViewModelAttribute<T> : AssetViewModelAttribute
{
    public override Type AssetType => typeof(T);
}
