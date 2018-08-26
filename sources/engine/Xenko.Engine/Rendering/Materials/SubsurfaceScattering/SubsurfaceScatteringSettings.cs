// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Data;

namespace Xenko.Rendering.Materials
{
    [DataContract]
    [Display("Subsurface Scattering Settings")]
    public class SubsurfaceScatteringSettings : Configuration
    {
        [DataMember(10)]
        public int SamplesPerScatteringKernel = 25;   // When this value is changed, all SSS materials need to be regenerated.

        public const int SamplesPerScatteringKernel2 = 25;  // TODO: Replace this by the above member!
    }
}