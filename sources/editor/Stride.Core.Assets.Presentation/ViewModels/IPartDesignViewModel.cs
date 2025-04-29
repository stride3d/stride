// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

/// <summary>
/// An interface for view models that contain a design part of <see cref="AssetComposite"/>.
/// </summary>
public interface IPartDesignViewModel<out TAssetPartDesign, TAssetPart>
    where TAssetPartDesign : IAssetPartDesign<TAssetPart>
    where TAssetPart : IIdentifiable
{
    /// <summary>
    /// Gets the part design object associated to the asset-side part.
    /// </summary>
    TAssetPartDesign PartDesign { get; }
}
