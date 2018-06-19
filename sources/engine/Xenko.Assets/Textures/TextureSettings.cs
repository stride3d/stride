// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Data;

namespace Xenko.Assets.Textures
{
    [DataContract]
    [Display("Textures")]
    public class TextureSettings : Configuration
    {
        public TextureSettings()
        {
            OfflineOnly = true;
        }

        /// <summary>
        /// Gets or sets the texture quality.
        /// </summary>
        /// <userdoc>The texture quality when encoding textures. Higher settings might result in much slower build depending on the target platform.</userdoc>
        [DataMember(0)]
        public TextureQuality TextureQuality = TextureQuality.Fast;
    }
}
