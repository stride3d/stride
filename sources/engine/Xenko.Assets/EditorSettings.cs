// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Data;

namespace Xenko.Assets
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
        /// The framerate at which Xenko displays animations. Animation data itself isn't affected.
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(30)]
        [DataMemberRange(1, 0)]
        public uint AnimationFrameRate = 30;
    }
}
