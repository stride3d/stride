// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Rendering.Images;
using Xenko.VirtualReality;

namespace Xenko.Rendering.Compositing
{
    [DataContract]
    public class VRRendererSettings
    {
        [DataMember(10)]
        public bool Enabled { get; set; }

        [DataMember(20)]
        [DefaultValue(true)]
        public bool IgnoreCameraRotation { get; set; } = true;

        [DataMember(25)]
        [DefaultValue(false)]
        public bool IgnoreDeviceRotation { get; set; } = false;

        [DataMember(27)]
        [DefaultValue(false)]
        public bool IgnoreDevicePosition { get; set; } = false;

        /// <summary>
        /// Specifies if VR rendering should be copied to the current render target.
        /// </summary>
        /// <userdoc>Copy VR rendering to the current render target. Leave disabled to have different rendering on desktop than VR headset.</userdoc>
        [DataMember(25)]
        [DefaultValue(true)]
        public bool CopyMirror { get; set; } = true;

        [DataMember(30)]
        public List<VRDeviceDescription> RequiredApis { get; } = new List<VRDeviceDescription>();

        [DataMember(40)]
        public List<VROverlayRenderer> Overlays { get; } = new List<VROverlayRenderer>();

        [DataMemberIgnore]
        public RenderView[] RenderViews = { new RenderView(), new RenderView() };

        [DataMemberIgnore]
        public VRDevice VRDevice;

        [DataMemberIgnore]
        public ImageScaler MirrorScaler = new ImageScaler();
    }
}
