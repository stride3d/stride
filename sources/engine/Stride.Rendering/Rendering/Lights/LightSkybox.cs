// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Skyboxes;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// A light coming from a skybox. The `BackgroundComponent` must be set on the entity in order to see a skybox.
    /// </summary>
    [DataContract("LightSkybox")]
    [Display("Skybox")]
    public class LightSkybox : IEnvironmentLight
    {
        /// <summary>
        /// Gets or sets the skybox.
        /// </summary>
        [DataMember(0)]
        public Skybox Skybox { get; set; }

        public float Distance = 250f, Curve = 0.5f, NearMip = 6.87f, FarMip = 0f;

        [DataMemberIgnore]
        internal Quaternion Rotation;

        public bool Update(RenderLight light)
        {
            Rotation = Quaternion.RotationMatrix(light.WorldMatrix);
            return Skybox != null;
        }
    }
}
