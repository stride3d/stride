// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// Stores the object reference information, so that it is easy to work on partially loaded or CPU version of assets with <see cref="ContentManager"/>.
    /// </summary>
    public class AttachedReference : IReference
    {
        /// <summary>
        /// The asset URL of the referenced data.
        /// </summary>
        public string Url;

        /// <summary>
        /// The asset unique identifier.
        /// </summary>
        public AssetId Id;

        /// <summary>
        /// If yes, this object won't be recursively saved in a separate chunk by <see cref="ContentManager"/>.
        /// Use this if you only care about the Url reference.
        /// </summary>
        public bool IsProxy;

        /// <summary>
        /// Data representation (useful if your object is a GPU object but you want to manipulate a CPU version of it).
        /// This needs to be manually interpreted by a custom <see cref="DataSerializer{T}"/> implementation.
        /// </summary>
        public object Data;

        AssetId IReference.Id => Id;

        string IReference.Location => Url;

        public override string ToString()
        {
            return Url;
        }
    }
}
