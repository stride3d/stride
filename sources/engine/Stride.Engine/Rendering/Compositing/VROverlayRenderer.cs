// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.VirtualReality;

namespace Stride.Rendering.Compositing
{
    [DataContract]
    public class VROverlayRenderer
    {
        [DataMember(10)]
        public Texture Texture;

        [DataMember(20)]
        public Vector3 LocalPosition;

        [DataMember(30)]
        public Quaternion LocalRotation = Quaternion.Identity;

        [DataMember(40)]
        public Vector2 SurfaceSize = Vector2.One;

        [DataMember(50)]
        public bool FollowsHeadRotation;

        [DataMemberIgnore]
        public VROverlay Overlay;
    }
}
