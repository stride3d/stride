// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A set of state values.
    /// </summary>
    public partial class StateInitializer : Expression
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "StateInitializer" /> class.
        /// </summary>
        public StateInitializer()
        {
            Items = new List<Expression>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the fields.
        /// </summary>
        /// <value>
        ///   The fields.
        /// </value>
        public List<Expression> Items { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            return Items;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "{...}";
        }

        #endregion
    }
}
