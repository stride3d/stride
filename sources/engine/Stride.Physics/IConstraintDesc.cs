// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Physics
{
    /// <summary>
    /// Description of a <see cref="Constraint"/>.
    /// </summary>
    public interface IConstraintDesc
    {
        /// <summary>
        /// Type of the constraint description.
        /// </summary>
        ConstraintTypes Type { get; }

        /// <summary>
        /// Position local to rigidbody A.
        /// </summary>
        Vector3 PivotInA { get; set; }

        /// <summary>
        /// Position local to rigidbody B.
        /// </summary>
        /// <remarks>
        /// Ignored when creating a body-world constraint.
        /// </remarks>
        Vector3 PivotInB { get; set; }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="rigidbodyA"></param>
        /// <param name="rigidbodyB"></param>
        /// <returns></returns>
        Constraint Build(RigidbodyComponent rigidbodyA, RigidbodyComponent rigidbodyB);
    }
}
