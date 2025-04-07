// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets;

/// <summary>
/// An interface representing a design-time part in an <see cref="AssetComposite"/>.
/// </summary>
public interface IAssetPartDesign
{
    BasePart? Base { get; set; }

    IIdentifiable Part { get; }
}

/// <summary>
/// An interface representing a design-time part in an <see cref="AssetComposite"/>.
/// </summary>
/// <typeparam name="TAssetPart">The underlying type of part.</typeparam>
public interface IAssetPartDesign<out TAssetPart> : IAssetPartDesign
    where TAssetPart : IIdentifiable
{
    /// <summary>
    /// The actual part.
    /// </summary>
    new TAssetPart Part { get; }
}
