// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Associates a type name used in YAML content.
    /// </summary>
    public class AssetAliasAttribute : Attribute
    {
        private readonly string alias;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetAliasAttribute"/> class.
        /// </summary>
        /// <param name="alias">The type name.</param>
        public AssetAliasAttribute(string @alias)
        {
            this.alias = alias;
        }

        /// <summary>
        /// Gets the type name.
        /// </summary>
        /// <value>The type name.</value>
        public string Alias
        {
            get
            {
                return alias;
            }
        }
    }
}
