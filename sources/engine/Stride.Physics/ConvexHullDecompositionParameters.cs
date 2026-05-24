// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
    /// <summary>
    /// How V-HACD fills the interior of the voxelized mesh during decomposition.
    /// </summary>
    [DataContract("VhacdFillMode")]
    public enum VhacdFillMode
    {
        /// <summary>Flood-fill from a known-outside cell to mark inside vs outside. Default; meshes with holes can fail.</summary>
        FloodFill = 0,
        /// <summary>Only the voxelized surface is kept; produces shell-only hulls with hollow interior.</summary>
        SurfaceOnly = 1,
        /// <summary>Uses raycasting to classify inside vs outside; more robust for meshes with holes.</summary>
        RaycastFill = 2,
    }

    [ContentSerializer(typeof(DataContentSerializer<ConvexHullDecompositionParameters>))]
    [DataContract("DecompositionParameters")]
    [Display("DecompositionParameters")]
    public class ConvexHullDecompositionParameters
    {
        /// <userdoc>
        /// If this is unchecked the following parameters are totally ignored, as only a simple convex hull of the whole model will be generated.
        /// </userdoc>
        public bool Enabled { get; set; }

        /// <userdoc>
        /// Maximum amount of shapes generated to fit the mesh. Higher values improve the fidelity at the cost of performance at runtime.
        /// </userdoc>
        [DataMember(60)]
        [DefaultValue(4)]
        public int MaxConvexHulls { get; set; } = 4;

        /// <userdoc>
        /// Maximum number of vertices allowed in any output convex hull (4 - 1024). Affects runtime performance and fidelity.
        /// </userdoc>
        [DataMember(65)]
        [DefaultValue(16)]
        public int MaxNumVerticesPerConvexHull
        {
            get => maxNumVerticesPerConvexHull;
            set => maxNumVerticesPerConvexHull = Math.Clamp(value, 4, 1024);
        }
        private int maxNumVerticesPerConvexHull = 16;

        /// <userdoc>
        /// Higher values restore the finer details of the mesh, but increase the time it takes to build the application.
        /// </userdoc>
        [DataMember(70)]
        [DefaultValue(400000)]
        public int Resolution
        {
            get => resolution;
            set => resolution = Math.Clamp(value, 10_000, 64_000_000);
        }
        private int resolution = 400000;

        /// <userdoc>
        /// Maximum recursion depth when splitting hulls (1 - 32). Greatly increases build time.
        /// </userdoc>
        [DataMember(80)]
        [DefaultValue(10)]
        public int MaxRecursionDepth
        {
            get => maxRecursionDepth;
            set => maxRecursionDepth = Math.Clamp(value, 1, 32);
        }
        private int maxRecursionDepth = 10;

        /// <userdoc>
        /// Merge shapes when the loss in precision of the volume is under this percentage. Affected by `Resolution`.
        /// </userdoc>
        [DataMember(90)]
        [DefaultValue(1.0)]
        public double MinimumVolumePercentErrorAllowed { get; set; } = 1.0;

        /// <userdoc>
        /// Snap hull vertices onto the source mesh surface for a tighter fit. Disable to leave them at the voxel grid (slightly larger than the source).
        /// </userdoc>
        [DataMember(100)]
        [DefaultValue(true)]
        public bool ShrinkWrap { get; set; } = true;

        /// <userdoc>
        /// Whether the mesh should be considered completely hollow (`SurfaceOnly`), entirely filled (`FloodFill`), or to guess (`RaycastFill`).
        /// </userdoc>
        [DataMember(110)]
        [DefaultValue(VhacdFillMode.FloodFill)]
        public VhacdFillMode FillMode { get; set; } = VhacdFillMode.FloodFill;

        public bool Match(object obj)
        {
            var other = obj as ConvexHullDecompositionParameters;

            if (other == null)
            {
                return false;
            }

            return other.Enabled == Enabled &&
                other.MaxConvexHulls == MaxConvexHulls &&
                other.MaxNumVerticesPerConvexHull == MaxNumVerticesPerConvexHull &&
                other.Resolution == Resolution &&
                other.MaxRecursionDepth == MaxRecursionDepth &&
                other.MinimumVolumePercentErrorAllowed == MinimumVolumePercentErrorAllowed &&
                other.ShrinkWrap == ShrinkWrap &&
                other.FillMode == FillMode;
        }
    }
}
