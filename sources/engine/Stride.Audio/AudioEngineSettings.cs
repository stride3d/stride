// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Data;

namespace Xenko.Audio
{
    [DataContract]
    [Display("Audio")]
    public class AudioEngineSettings : Configuration
    {
        /// <summary>
        /// Enables HRTF audio. Note that only audio emitters with HRTF enabled produce HRTF audio
        /// </summary>
        /// <userdoc>
        /// Enables HRTF audio. Note that only audio emitters with HRTF enabled produce HRTF audio
        /// </userdoc>
        [DataMember(10)]
        [Display("HRTF (if available)")]
        public bool HrtfSupport { get; set; }
    }
}
