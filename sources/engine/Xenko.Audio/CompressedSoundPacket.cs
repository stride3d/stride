// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Audio
{
    /// <summary>
    /// Used internally in SoundAsset
    /// </summary>
    [DataContract("CompressedSoundPacket")]
    public class CompressedSoundPacket
    {
        /// <summary>
        /// The length of the Data.
        /// </summary>
        public int Length;

        /// <summary>
        /// The Data.
        /// </summary>
        public byte[] Data;
    }
}
