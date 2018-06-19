// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// Describes parameters for building a package
    /// </summary>
    [DataContract("PackageBuildConfiguration")]
    public sealed class PackageBuildConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageBuildConfiguration"/> class.
        /// </summary>
        public PackageBuildConfiguration()
        {
            Profiles = new Dictionary<string, PackageProfile>();
        }

        /// <summary>
        /// Gets the profiles.
        /// </summary>
        /// <value>The profiles.</value>
        public Dictionary<string, PackageProfile> Profiles { get; private set; }
    }
}
