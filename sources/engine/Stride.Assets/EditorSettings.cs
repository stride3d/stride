// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Data;

namespace Stride.Assets
{
    [DataContract]
    [Display("Editor")]
    public class EditorSettings : Configuration
    {
        public EditorSettings()
        {
            OfflineOnly = true;
        }

        /// <userdoc>
        /// Applies to thumbnails and asset previews
        /// </userdoc>
        [DataMember(0)]
        public RenderingMode RenderingMode = RenderingMode.HDR;

        /// <userdoc>
        /// The framerate at which Stride displays animations. Animation data itself isn't affected.
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(30)]
        [DataMemberRange(1, 0)]
        public uint AnimationFrameRate = 30;
    }
}
