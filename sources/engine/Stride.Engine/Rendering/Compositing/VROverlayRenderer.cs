// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.VirtualReality;

namespace Xenko.Rendering.Compositing
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
