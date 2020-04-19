// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Toplevel container of a shader parsing result.
    /// </summary>
    public partial class Shader : Node
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Shader" /> class.
        /// </summary>
        public Shader()
        {
            Declarations = new List<Node>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the declarations.
        /// </summary>
        /// <value>
        ///   The declarations.
        /// </value>
        public List<Node> Declarations { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            return Declarations;
        }

        #endregion
    }
}
