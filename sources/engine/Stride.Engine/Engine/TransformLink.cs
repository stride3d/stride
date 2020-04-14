// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;

namespace Stride.Engine
{
    /// <summary>
    /// Gives the ability to control how parent matrix is computed in a <see cref="TransformComponent"/>.
    /// </summary>
    public abstract class TransformLink
    {
        /// <summary>
        /// Compute a world matrix this link represents.
        /// </summary>
        /// <param name="recursive"></param>
        /// <param name="matrix">The computed world matrix.</param>
        public abstract void ComputeMatrix(bool recursive, out Matrix matrix);
    }
}
