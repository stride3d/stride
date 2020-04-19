// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A base implementation of the <see cref="IAssetFactory{T}"/> interface.
    /// </summary>
    /// <typeparam name="T">The type of asset this factory can create.</typeparam>
    public abstract class AssetFactory<T> : IAssetFactory<T> where T : Asset
    {
        /// <inheritdoc/>
        public Type AssetType => typeof(T);

        /// <inheritdoc/>
        public abstract T New();
    }
}
