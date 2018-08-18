// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// Describes a rasterizer state.
    /// </summary>
    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public struct RasterizerStateDescription : IEquatable<RasterizerStateDescription>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RasterizerStateDescription"/> class.
        /// </summary>
        /// <param name="cullMode">The cull mode.</param>
        public RasterizerStateDescription(CullMode cullMode) : this()
        {
            SetDefault();
            CullMode = cullMode;
        }

        /// <summary>
        /// Determines the fill mode to use when rendering (see <see cref="FillMode"/>).
        /// </summary>
        public FillMode FillMode;

        /// <summary>
        /// Indicates triangles facing the specified direction are not drawn (see <see cref="CullMode"/>).
        /// </summary>
        public CullMode CullMode;

        /// <summary>
        /// Determines if a triangle is front- or back-facing. If this parameter is true, then a triangle will be considered front-facing if its vertices are counter-clockwise on the render target and considered back-facing if they are clockwise. If this parameter is false then the opposite is true.
        /// </summary>
        public bool FrontFaceCounterClockwise;

        /// <summary>
        /// Depth value added to a given pixel. 
        /// </summary>
        public int DepthBias;

        /// <summary>
        /// Gets or sets the depth bias for polygons, which is the amount of bias to apply to the depth of a primitive to alleviate depth testing problems for primitives of similar depth. The default value is 0.
        /// </summary>
        public float DepthBiasClamp;

        /// <summary>
        /// Scalar on a given pixel's slope. 
        /// </summary>
        public float SlopeScaleDepthBias;

        /// <summary>
        /// Enable clipping based on distance. 
        /// </summary>
        public bool DepthClipEnable;

        /// <summary>
        /// Enable scissor-rectangle culling. All pixels ouside an active scissor rectangle are culled.
        /// </summary>
        public bool ScissorTestEnable;

        /// <summary>
        /// Multisample level.
        /// </summary>
        public MultisampleCount MultisampleCount;

        /// <summary>
        /// Enable line antialiasing; only applies if doing line drawing and MultisampleEnable is false.
        /// </summary>
        public bool MultisampleAntiAliasLine;

        /// <summary>
        /// Sets default values for this instance.
        /// </summary>
        public void SetDefault()
        {
            CullMode = CullMode.Back;
            FillMode = FillMode.Solid;
            DepthClipEnable = true;
            FrontFaceCounterClockwise = false;
            ScissorTestEnable = false;
            MultisampleCount = MultisampleCount.None;
            MultisampleAntiAliasLine = false;
            DepthBias = 0;
            DepthBiasClamp = 0f;
            SlopeScaleDepthBias = 0f;
        }

        /// <summary>
        /// Gets default values for this instance.
        /// </summary>
        public static RasterizerStateDescription Default
        {
            get
            {
                var desc = new RasterizerStateDescription();
                desc.SetDefault();
                return desc;
            }
        }

        public bool Equals(RasterizerStateDescription other)
        {
            return FillMode == other.FillMode && CullMode == other.CullMode && FrontFaceCounterClockwise == other.FrontFaceCounterClockwise && DepthBias == other.DepthBias && DepthBiasClamp.Equals(other.DepthBiasClamp) && SlopeScaleDepthBias.Equals(other.SlopeScaleDepthBias) && DepthClipEnable == other.DepthClipEnable && ScissorTestEnable == other.ScissorTestEnable && MultisampleCount == other.MultisampleCount && MultisampleAntiAliasLine == other.MultisampleAntiAliasLine;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is RasterizerStateDescription && Equals((RasterizerStateDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)FillMode;
                hashCode = (hashCode * 397) ^ (int)CullMode;
                hashCode = (hashCode * 397) ^ FrontFaceCounterClockwise.GetHashCode();
                hashCode = (hashCode * 397) ^ DepthBias;
                hashCode = (hashCode * 397) ^ DepthBiasClamp.GetHashCode();
                hashCode = (hashCode * 397) ^ SlopeScaleDepthBias.GetHashCode();
                hashCode = (hashCode * 397) ^ DepthClipEnable.GetHashCode();
                hashCode = (hashCode * 397) ^ ScissorTestEnable.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)MultisampleCount;
                hashCode = (hashCode * 397) ^ MultisampleAntiAliasLine.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(RasterizerStateDescription left, RasterizerStateDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RasterizerStateDescription left, RasterizerStateDescription right)
        {
            return !left.Equals(right);
        }
    }
}
