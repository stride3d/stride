// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// Base class for a shadow map.
    /// </summary>
    [DataContract("LightShadowMap")]
    [Display("ShadowMap")]
    public abstract class LightShadowMap : ILightShadow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LightShadowMap"/> class.
        /// </summary>
        protected LightShadowMap()
        {
            Enabled = false;
            Size = LightShadowMapSize.Medium;
            BiasParameters = new ShadowMapBiasParameters();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="LightShadowMap"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        /// <userdoc>Display the shadow of the engendered by the light</userdoc>
        [DataMember(10)]
        [DefaultValue(false)]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the shadow map filtering.
        /// </summary>
        /// <value>The filter type.</value>
        /// <userdoc>The type of filter to apply onto the shadow.</userdoc>
        [DataMember(20)]
        [DefaultValue(null)]
        public ILightShadowMapFilterType Filter { get; set; }

        /// <summary>
        /// Gets or sets the size of the shadow-map.
        /// </summary>
        /// <value>The size.</value>
        /// <userdoc>The size of texture to use for shadow mapping. Large textures produces better shadows edges but are much more costly.</userdoc>
        [DataMember(30)]
        [DefaultValue(LightShadowMapSize.Medium)]
        public LightShadowMapSize Size { get; set; }

        /// <summary>
        /// Gets the bias parameters.
        /// </summary>
        /// <value>The bias parameters.</value>
        /// <userdoc>Offset values to add during to the depth calculation process of the shadow map.</userdoc>
        [DataMember(100)]
        [Display("Bias Parameters", Expand = ExpandRule.Always)]
        public ShadowMapBiasParameters BiasParameters { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="LightShadowMap"/> is debug.
        /// </summary>
        /// <value><c>true</c> if debug; otherwise, <c>false</c>.</value>
        /// <userdoc>Render the shadow map in debug mode</userdoc>
        [DataMember(200)]
        [DefaultValue(false)]
        public bool Debug { get; set; }

        public virtual int GetCascadeCount()
        {
            return 1;
        }

        /// <summary>
        /// Bias parameters used for shadow map.
        /// </summary>
        [DataContract("LightShadowMap.ShadowMapBiasParameters")]
        public sealed class ShadowMapBiasParameters
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ShadowMapBiasParameters"/> class.
            /// </summary>
            public ShadowMapBiasParameters()
            {
                DepthBias = 0.01f;
                NormalOffsetScale = 10.0f;
            }

            /// <summary>
            /// Gets or sets the depth bias used for shadow map comparison.
            /// </summary>
            /// <value>The bias.</value>
            /// <userdoc>An absolute value to add to the calculated depth.</userdoc>
            [DataMember(10)]
            [DefaultValue(0.01f)]
            public float DepthBias { get; set; }

            /// <summary>
            /// Gets or sets the offset scale in world space unit along the surface normal.
            /// </summary>
            /// <value>The offset scale.</value>
            /// <userdoc>A factor specifying the offset to add to the calculated depth with respect to the surface normal.</userdoc>
            [DataMember(20)]
            [DefaultValue(10.0f)]
            public float NormalOffsetScale { get; set; }
        }
    }
}
