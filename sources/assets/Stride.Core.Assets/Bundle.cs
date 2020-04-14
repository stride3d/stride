// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Stride.Core;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Description of an asset bundle.
    /// </summary>
    [DataContract("Bundle")]
    [DebuggerDisplay("Bundle [{Name}] Selectors[{Selectors.Count}] Dependencies[{Dependencies.Count}]")]
    public sealed class Bundle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Bundle"/> class.
        /// </summary>
        public Bundle()
        {
            Selectors = new List<AssetSelector>();
            Dependencies = new List<string>();
        }

        /// <summary>
        /// Gets or sets the name of this bundle.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets the selectors used by this bundle.
        /// </summary>
        /// <value>The selectors.</value>
        public List<AssetSelector> Selectors { get; private set; }

        /// <summary>
        /// Gets the bundle dependencies.
        /// </summary>
        /// <value>The dependencies.</value>
        public List<string> Dependencies { get; private set; }

        /// <summary>
        /// Gets the output group (used in conjonction with <see cref="ProjectBuildProfile.OutputGroupDirectories"/> to control where file will be put).
        /// </summary>
        /// <value>
        /// The output group.
        /// </value>
        [DefaultValue(null)]
        public string OutputGroup { get; set; }
    }
}
