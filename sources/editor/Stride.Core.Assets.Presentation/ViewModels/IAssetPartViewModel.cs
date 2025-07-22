// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Presentation.ViewModels;

/// <summary>
/// An interface for view models that represents asset parts of <see cref="AssetComposite"/>.
/// </summary>
/// // TODO: replace with IIdentifiable
public interface IAssetPartViewModel : IDestroyable
{
    /// <summary>
    /// Gets the id of this part.
    /// </summary>
    AbsoluteId Id { get; }
}
