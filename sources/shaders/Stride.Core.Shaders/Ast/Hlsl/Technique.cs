// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// Technique description.
    /// </summary>
    public partial class Technique : Node, IDeclaration, IAttributes
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Technique" /> class.
        /// </summary>
        public Technique()
        {
            Attributes = new List<AttributeBase>();
            Passes = new List<Pass>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public Identifier Type { get; set; }

        /// <summary>
        ///   Gets or sets the attributes.
        /// </summary>
        /// <value>
        ///   The attributes.
        /// </value>
        public List<AttributeBase> Attributes { get; set; }

        /// <summary>
        ///   Gets or sets the name.
        /// </summary>
        /// <value>
        ///   The name.
        /// </value>
        public Identifier Name { get; set; }

        /// <summary>
        ///   Gets or sets the passes.
        /// </summary>
        /// <value>
        ///   The passes.
        /// </value>
        public List<Pass> Passes { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.AddRange(Attributes);
            ChildrenList.Add(Name);
            ChildrenList.AddRange(Passes);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("technique {0}{{...}}", Name != null ? Name + " " : string.Empty);
        }

        #endregion
    }
}
