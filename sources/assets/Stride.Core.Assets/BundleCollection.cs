// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A collection of bundles.
    /// </summary>
    [DataContract("!Bundles")]
    public class BundleCollection : List<Bundle>
    {
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="BundleCollection"/> class.
        /// </summary>
        /// <param name="package">The package.</param>
        internal BundleCollection(Package package)
        {
            this.package = package;
        }

        /// <summary>
        /// Gets the package this bundle collection is defined for.
        /// </summary>
        /// <value>The package.</value>
        [DataMemberIgnore]
        private Package Package
        {
            get
            {
                return package;
            }
        }
    }
}
