// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.Annotations;

public abstract class AssetViewModelAttribute : Attribute
{
    public abstract Type AssetType { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class AssetViewModelAttribute<T> : AssetViewModelAttribute
{
    public override Type AssetType => typeof(T);
}
