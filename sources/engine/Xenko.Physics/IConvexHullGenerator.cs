// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Physics
{
    public interface IConvexHullGenerator
    {
        bool Match(object obj);
    }
    public interface IVhacdConvexHullGenerator : IConvexHullGenerator
    {
        /// <userdoc>
        /// If this is checked the following parameters are totally ignored, as only a simple convex hull of the whole model will be generated.
        /// </userdoc>
        bool SimpleHull { get; }

        /// <userdoc>
        /// Control how many sub convex hulls will be created, more depth will result in a more complex decomposition.
        /// </userdoc>
        int Depth { get; }

        /// <userdoc>
        /// How many position samples to internally compute clipping planes ( the higher the more complex ).
        /// </userdoc>
        int PosSampling { get; }

        /// <userdoc>
        /// How many angle samples to internally compute clipping planes ( the higher the more complex ), nested with position samples, for each position sample it will compute the amount defined here.
        /// </userdoc>
        int AngleSampling { get; }

        /// <userdoc>
        /// If higher then 0 the computation will try to further improve the shape position sampling (this will slow down the process).
        /// </userdoc>
        int PosRefine { get; }

        /// <userdoc>
        /// If higher then 0 the computation will try to further improve the shape angle sampling (this will slow down the process).
        /// </userdoc>
        int AngleRefine { get; }

        /// <userdoc>
        /// Applied to the concavity during crippling plane approximation.
        /// </userdoc>
        float Alpha { get; }

        /// <userdoc>
        /// Threshold of concavity, rising this will make the shape simpler.
        /// </userdoc>
        float Threshold { get; }
    }
}
