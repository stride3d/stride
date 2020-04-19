// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Serialization.Contents;

namespace Stride.Physics
{
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
        /// Control how many sub convex hulls will be created, more depth will result in a more complex decomposition.
        /// </userdoc>
        [DataMember(60)]
        public int Depth { get; set; } = 10;

        /// <userdoc>
        /// How many position samples to internally compute clipping planes ( the higher the more complex ).
        /// </userdoc>
        [DataMember(70)]
        public int PosSampling { get; set; } = 10;

        /// <userdoc>
        /// How many angle samples to internally compute clipping planes ( the higher the more complex ), nested with position samples, for each position sample it will compute the amount defined here.
        /// </userdoc>
        [DataMember(80)]
        public int AngleSampling { get; set; } = 10;

        /// <userdoc>
        /// If higher then 0 the computation will try to further improve the shape position sampling (this will slow down the process).
        /// </userdoc>
        [DataMember(90)]
        public int PosRefine { get; set; } = 5;

        /// <userdoc>
        /// If higher then 0 the computation will try to further improve the shape angle sampling (this will slow down the process).
        /// </userdoc>
        [DataMember(100)]
        public int AngleRefine { get; set; } = 5;

        /// <userdoc>
        /// Applied to the concavity during crippling plane approximation.
        /// </userdoc>
        [DataMember(110)]
        public float Alpha { get; set; } = 0.01f;

        /// <userdoc>
        /// Threshold of concavity, rising this will make the shape simpler.
        /// </userdoc>
        [DataMember(120)]
        public float Threshold { get; set; } = 0.01f;

        public bool Match(object obj)
        {
            var other = obj as ConvexHullDecompositionParameters;

            if (other == null)
            {
                return false;
            }

            return other.Enabled == Enabled &&
                other.Depth == Depth &&
                other.PosSampling == PosSampling &&
                other.AngleSampling == AngleSampling &&
                other.PosRefine == PosRefine &&
                other.AngleRefine == AngleRefine &&
                Math.Abs(other.Alpha - Alpha) < float.Epsilon &&
                Math.Abs(other.Threshold - Threshold) < float.Epsilon;
        }
    }
}
