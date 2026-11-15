// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Annotations;

/// <summary>
/// Declares a fixed thumbnail image for <see cref="AssetType"/>, an embedded resource of the attributed assembly
/// (the asset's runtime, Assets or Editor package). <see cref="ResourceName"/> is the full manifest resource name or
/// its end, such as <c>VideoThumbnail.png</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class StaticThumbnailAttribute : Attribute
{
    public StaticThumbnailAttribute(Type assetType, string resourceName)
    {
        AssetType = assetType;
        ResourceName = resourceName;
    }

    public Type AssetType { get; }

    public string ResourceName { get; }
}
