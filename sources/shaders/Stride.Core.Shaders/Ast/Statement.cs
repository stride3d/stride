// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Base root class for all statements.
    /// </summary>
    public abstract partial class Statement : Node, IAttributes
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Statement"/> class.
        /// </summary>
        protected Statement()
        {
            Attributes = new List<AttributeBase>();
        }

        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        public List<AttributeBase> Attributes { get; set; }
    }
}
