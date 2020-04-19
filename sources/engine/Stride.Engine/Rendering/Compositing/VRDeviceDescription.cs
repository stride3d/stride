// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Rendering.Images;
using Stride.VirtualReality;

namespace Stride.Rendering.Compositing
{
    [DataContract]
    public class VRDeviceDescription
    {
        [DataMember(10)]
        public VRApi Api { get; set; }

        [DataMember(20)]
        public float ResolutionScale { get; set; } = 1.0f;
    }
}
