// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Core.Assets
{
    /// <summary>
    /// An interface representing a design-time part in an <see cref="AssetComposite"/>.
    /// </summary>
    public interface IAssetPartDesign
    {
        [CanBeNull]
        BasePart Base { get; set; }

        [NotNull]
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
        [NotNull]
        new TAssetPart Part { get; }
    }
}
