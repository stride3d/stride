// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Assets
{
    /// <summary>
    /// An interface that represents an asset factory.
    /// </summary>
    /// <typeparam name="T">The type of asset this factory can create.</typeparam>
    [AssemblyScan]
    public interface IAssetFactory<out T> where T : Asset
    {
        /// <summary>
        /// Retrieve the asset type associated to this factory.
        /// </summary>
        /// <returns>The asset type associated to this factory.</returns>
        [NotNull]
        Type AssetType { get; }

        /// <summary>
        /// Creates a new instance of the asset type associated to this factory.
        /// </summary>
        /// <returns>A new instance of the asset type associated to this factory.</returns>
        T New();
    }
}
