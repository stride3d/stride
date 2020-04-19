// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Packages
{
    /// <summary>
    /// Collection of constraints associated to some packages expressed as version ranges.
    /// </summary>
    public class ConstraintProvider
    {
        /// <summary>
        /// Store <see cref="PackageVersionRange"/> constraints associated to a given package.
        /// </summary>
        private readonly Dictionary<string, PackageVersionRange> constraints = new Dictionary<string, PackageVersionRange>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Does current instance have constraints?
        /// </summary>
        public bool HasConstraints => constraints.Count > 0;

        /// <summary>
        /// Add constraint <paramref name="range"/> to package ID <paramref name="packageId"/>.
        /// </summary>
        /// <param name="packageId">Package on which constraint <paramref name="range"/> will be applied.</param>
        /// <param name="range">Range of constraint.</param>
        public void AddConstraint(string packageId, PackageVersionRange range)
        {
            constraints[packageId] = range;
        }

        /// <summary>
        /// Retrieve constraint associated with <paramref name="packageId"/> if any.
        /// </summary>
        /// <param name="packageId">Id of package being queried.</param>
        /// <returns>Constraint if any, null otherwise.</returns>
        internal PackageVersionRange GetConstraint(string packageId)
        {
            PackageVersionRange versionRange;
            if (constraints.TryGetValue(packageId, out versionRange))
            {
                return versionRange;
            }
            return null;
        }
    }
}
