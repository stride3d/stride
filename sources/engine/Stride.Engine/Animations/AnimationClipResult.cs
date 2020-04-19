// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Updater;

namespace Stride.Animations
{
    public class AnimationClipResult
    {
        private static readonly byte[] EmptyData = new byte[0];

        // Future use, when object will be supported.
        // private object[] objects;

        /// <summary>
        /// Total size of all structures to be stored in structures.
        /// </summary>
        public int DataSize;

        /// <summary>
        /// Gets or sets the animation channel descriptions.
        /// </summary>
        /// <value>
        /// The animation channel descriptions.
        /// </value>
        public List<AnimationBlender.Channel> Channels { get; set; }

        /// <summary>
        /// Stores all animation channel blittable struct at a given time.
        /// </summary>
        public byte[] Data = EmptyData;

        /// <summary>
        /// Stores all animation channel objects and non-blittable struct at a given time.
        /// </summary>
        public UpdateObjectData[] Objects;
    }
}
