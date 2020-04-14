// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Data;

namespace Stride.Assets.Textures
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
