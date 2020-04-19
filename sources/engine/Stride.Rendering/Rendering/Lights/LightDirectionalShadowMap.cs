// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// A directional shadow map.
    /// </summary>
    [DataContract("LightDirectionalShadowMap")]
    [Display("Directional ShadowMap")]
    public class LightDirectionalShadowMap : LightShadowMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LightShadowMap"/> class.
        /// </summary>
        public LightDirectionalShadowMap()
        {
            CascadeCount = LightShadowMapCascadeCount.FourCascades;
            DepthRange = new DepthRangeParameters();
            PartitionMode = new PartitionLogarithmic();
            StabilizationMode = LightShadowMapStabilizationMode.ProjectionSnapping;
        }

        /// <summary>
        /// Gets or Sets the number of cascades for this shadow (valid only for directional lights)
        /// </summary>
        /// <value>The number of cascades for this shadow.</value>
        /// <userdoc>The number of cascades for the shadows.</userdoc>
        [DataMember(50)]
        [DefaultValue(LightShadowMapCascadeCount.FourCascades)]
        public LightShadowMapCascadeCount CascadeCount { get; set; }

        /// <summary>
        /// Gets or Sets the stabilization mode of the shadow map
        /// </summary>
        /// <userdoc>The technique used to stabilize the shadow map</userdoc>
        [DataMember(60)]
        [DefaultValue(LightShadowMapStabilizationMode.ProjectionSnapping)]
        public LightShadowMapStabilizationMode StabilizationMode { get; set; }

        /// <summary>
        /// Gets or sets the depth range mode.
        /// </summary>
        /// <value>The depth range mode.</value>
        [DataMember(80)]
        [NotNull]
        [Display("Depth Range", Expand = ExpandRule.Always)]
        public DepthRangeParameters DepthRange { get; private set; }

        /// <summary>
        /// Gets or sets the partition mode.
        /// </summary>
        /// <value>The partition mode.</value>
        [DataMember(90)]
        [NotNull]
        public PartitionModeBase PartitionMode { get; set; }

        /// <summary>
        /// Specifies if transmittance should be computed (necessary for hair and skin rendering).
        /// </summary>
        /// <userdoc>Specifies if transmittance should be computed (necessary for hair and skin rendering).</userdoc>
        [DataMember(100)]
        public bool ComputeTransmittance { get; set; }

        public override int GetCascadeCount()
        {
            return (int)CascadeCount;
        }

        /// <summary>
        /// Base class for the partition mode.
        /// </summary>
        [DataContract]
        public abstract class PartitionModeBase
        {
        }

        /// <summary>
        /// Manual partition. This class cannot be inherited.
        /// </summary>
        [DataContract("LightDirectionalShadowMap.PartitionManual")]
        [Display("Manual")]
        public sealed class PartitionManual : PartitionModeBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LightDirectionalManualPartitionMode"/> class.
            /// </summary>
            public PartitionManual()
            {
                SplitDistance0 = 0.05f;
                SplitDistance1 = 0.15f;
                SplitDistance2 = 0.50f;
                SplitDistance3 = 1.00f;
            }

            [DataMember(10)]
            [DefaultValue(0.05f)]
            [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
            public float SplitDistance0 { get; set; }

            [DataMember(20)]
            [DefaultValue(0.15f)]
            [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
            public float SplitDistance1 { get; set; }

            [DataMember(30)]
            [DefaultValue(0.5f)]
            [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
            public float SplitDistance2 { get; set; }

            [DataMember(40)]
            [DefaultValue(1.0f)]
            [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
            public float SplitDistance3 { get; set; }
        }

        /// <summary>
        /// Logarithmic and PSSM partition. This class cannot be inherited.
        /// </summary>
        [DataContract("LightDirectionalShadowMap.PartitionLogarithmic")]
        [Display("Logarithmic")]
        public sealed class PartitionLogarithmic : PartitionModeBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PartitionLogarithmic"/> class.
            /// </summary>
            public PartitionLogarithmic()
            {
                PSSMFactor = 0.5f;
            }

            /// <summary>
            /// Gets or sets the PSSM factor (0.0f is full logarithmic, 1.0f is full PSSM).
            /// </summary>
            /// <value>The PSSM factor.</value>
            [DataMember(10)]
            [DefaultValue(0.5f)]
            [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
            [Display("PSSM")]
            public float PSSMFactor { get; set; }
        }

        /// <summary>
        /// The depth range is set manually. This class cannot be inherited.
        /// </summary>
        [DataContract("LightDirectionalShadowMap.DepthRangeParameters")]
        [Display("Depth Range Parameters")]
        public sealed class DepthRangeParameters
        {
            /// <summary>
            /// The default minimum distance
            /// </summary>
            public const float DefaultMinDistance = 0.0f;

            /// <summary>
            /// The default guard distance for the auto mode.
            /// </summary>
            public const float DefaultGuardDistance = 1.0f;

            /// <summary>
            /// The default maximum distance
            /// </summary>
            public const float DefaultMaxDistance = 50.0f;

            /// <summary>
            /// Initializes a new instance of the <see cref="DepthRangeParameters"/> class.
            /// </summary>
            public DepthRangeParameters()
            {
                IsAutomatic = true;
                ManualMinDistance = DefaultMinDistance;
                ManualMaxDistance = DefaultMaxDistance;
                IsBlendingCascades = true;
            }

            /// <summary>
            /// Gets or sets a value indicating whether this instance is automatic.
            /// </summary>
            /// <value><c>true</c> if this instance is automatic; otherwise, <c>false</c>.</value>
            [DataMember(0)]
            [DefaultValue(true)]
            [Display("Automatic?")]
            public bool IsAutomatic { get; set; }

            /// <summary>
            /// Gets or sets the minimum distance in world space unit.
            /// </summary>
            /// <value>The minimum distance.</value>
            [DataMember(10)]
            [DefaultValue(DefaultMinDistance)]
            public float ManualMinDistance { get; set; }

            /// <summary>
            /// Gets or sets the maximum distance in world space unit.
            /// </summary>
            /// <value>The maximum distance.</value>
            [DataMember(20)]
            [DefaultValue(DefaultMaxDistance)]
            public float ManualMaxDistance { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this instance is filtering accross cascades.
            /// </summary>
            /// <value><c>true</c> if this instance is filtering accross cascades; otherwise, <c>false</c>.</value>
            [DataMember(30)]
            [DefaultValue(true)]
            [Display("Blend Cascades?")]
            public bool IsBlendingCascades { get; set; }
        }
    }
}
