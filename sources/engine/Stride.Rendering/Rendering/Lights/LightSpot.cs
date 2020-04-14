// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// A spot light.
    /// </summary>
    [DataContract("LightSpot")]
    [Display("Spot")]
    public class LightSpot : DirectLightBase
    {
        // These values have to match the ones defined in "TextureProjectionReceiverBase.sdsl".
        public enum FlipModeEnum
        {
            None = 0,
            FlipX = 1,
            FlipY = 2,
            FlipXY = 3,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightSpot"/> class.
        /// </summary>
        public LightSpot()
        {
            Range = 3.0f;
            AngleInner = 30.0f;
            AngleOuter = 35.0f;
            UVOffset = Vector2.Zero;
            UVScale = Vector2.One;
            Shadow = new LightStandardShadowMap()
            {
                Size = LightShadowMapSize.Medium,
                BiasParameters =
                {
                    DepthBias = 0.001f,
                },
            };
        }

        /// <summary>
        /// Gets or sets the range distance the light is affecting.
        /// </summary>
        /// <value>The range.</value>
        /// <userdoc>The range of the spot light in scene units</userdoc>
        [DataMember(10)]
        [DefaultValue(3.0f)]
        public float Range { get; set; }

        /// <summary>
        /// Gets or sets the spot angle in degrees.
        /// </summary>
        /// <value>The spot angle in degrees.</value>
        /// <userdoc>The angle of the main beam of the light spot.</userdoc>
        [DataMember(20)]
        [DataMemberRange(0.01, 90, 1, 10, 1)]
        [DefaultValue(30.0f)]
        public float AngleInner { get; set; }

        /// <summary>
        /// Gets or sets the spot angle in degrees.
        /// </summary>
        /// <value>The spot angle in degrees.</value>
        /// <userdoc>The angle of secondary beam of the light spot</userdoc>
        [DataMember(30)]
        [DataMemberRange(0.01, 90, 1, 10, 1)]
        [DefaultValue(35.0f)]
        public float AngleOuter { get; set; }

        /// <summary>The texture that is multiplied on top of the lighting result like a mask. Can be used like a cinema projector.</summary>
        /// <userdoc>The texture that is multiplied on top of the lighting result like a mask. Can be used like a cinema projector.</userdoc>
        [DataMember(40)]
        [DefaultValue(null)]
        public Texture ProjectiveTexture { get; set; }

        /// <summary>
        /// The scale of the texture coordinates.
        /// </summary>
        /// <userdoc>
        /// The scale to apply to the texture coordinates. Values lower than 1 zoom the texture in; values greater than 1 tile it.
        /// </userdoc>
        [DataMember(43)]
        [Display("UV scale")]
        public Vector2 UVScale { get; set; }

        /// <summary>
        /// The offset in the texture coordinates.
        /// </summary>
        /// <userdoc>
        /// The offset to apply to the model's texture coordinates
        /// </userdoc>
        [DataMember(45)]
        [Display("UV offset")]
        public Vector2 UVOffset { get; set; }

        /// <summary>Scales the mip map level in the shader. 0 = biggest mip map, 1 = smallest mip map.</summary>
        /// <userdoc>Used to set how blurry the projected texture should be. 0 = full resolution, 1 = 1x1 pixel</userdoc>
        [DataMember(50)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        [DefaultValue(0.0f)]
        public float MipMapScale { get; set; } = 0.0f;

        [DataMember(60)]
        [DefaultValue(1.0f)]
        public float AspectRatio { get; set; } = 1.0f;

        [DataMember(70)]
        [DefaultValue(0.2f)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        public float TransitionArea { get; set; } = 0.2f;

        [DataMember(75)]
        [DefaultValue(1.0f)]
        public float ProjectionPlaneDistance { get; set; } = 1.0f;

        [DataMember(80)]
        [DefaultValue(FlipModeEnum.None)]
        public FlipModeEnum FlipMode { get; set; } = FlipModeEnum.None;

        [DataMemberIgnore]
        internal float InvSquareRange;

        [DataMemberIgnore]
        internal float LightAngleScale;

        [DataMemberIgnore]
        internal float AngleOuterInRadians;

        [DataMemberIgnore]
        internal float LightAngleOffset;

        internal float LightRadiusAtTarget;

        public override bool Update(RenderLight light)
        {
            var range = Math.Max(0.001f, Range);
            InvSquareRange = 1.0f / (range * range);
            var innerAngle = Math.Min(AngleInner, AngleOuter);
            var outerAngle = Math.Max(AngleInner, AngleOuter);
            AngleOuterInRadians = MathUtil.DegreesToRadians(outerAngle);
            var cosInner = (float)Math.Cos(MathUtil.DegreesToRadians(innerAngle / 2));
            var cosOuter = (float)Math.Cos(AngleOuterInRadians * 0.5f);
            LightAngleScale = 1.0f / Math.Max(0.001f, cosInner - cosOuter);
            LightAngleOffset = -cosOuter * LightAngleScale;

            LightRadiusAtTarget = (float)Math.Abs(Range * Math.Sin(AngleOuterInRadians * 0.5f));

            return true;
        }

        public override bool HasBoundingBox
        {
            get
            {
                return true;
            }
        }

        public override BoundingBox ComputeBounds(Vector3 position, Vector3 direction)
        {
            // Calculates the bouding box of the spot target
            var spotTarget = position + direction * Range;
            var r = LightRadiusAtTarget * 1.73205080f; // * length(vector3(r,r,r))
            var box = new BoundingBox(spotTarget - r, spotTarget + r);

            // Merge it with the start of the bounding box
            BoundingBox.Merge(ref box, ref position, out box);
            return box;
        }

        public override float ComputeScreenCoverage(RenderView renderView, Vector3 position, Vector3 direction)
        {
            // TODO: We could improve this by calculating a screen-aligned triangle and a sphere at the end of the cone.
            //       With the screen-aligned triangle we would cover the entire spotlight, not just its end.

            // http://stackoverflow.com/questions/21648630/radius-of-projected-sphere-in-screen-space
            // Use a sphere at target point to compute the screen coverage. This is a very rough approximation.
            // We compute the sphere at target point where the size of light is the largest
            // TODO: Check if we can improve this calculation with a better model
            var targetPosition = new Vector4(position + direction * Range, 1.0f);
            Vector4 projectedTarget;
            Vector4.Transform(ref targetPosition, ref renderView.ViewProjection, out projectedTarget);

            var d = Math.Abs(projectedTarget.W) + 0.00001f;
            var r = Range * Math.Sin(MathUtil.DegreesToRadians(AngleOuter / 2.0f));

            // Handle correctly the case where the eye is inside the sphere
            if (d < r)
                return Math.Max(renderView.ViewSize.X, renderView.ViewSize.Y);

            var coTanFovBy2 = renderView.Projection.M22;
            var pr = r * coTanFovBy2 / (Math.Sqrt(d * d - r * r) + 0.00001f);

            // Size on screen
            return (float)pr * Math.Max(renderView.ViewSize.X, renderView.ViewSize.Y);
        }
    }
}
