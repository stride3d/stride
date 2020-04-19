// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// Declaration of a constant buffer.
    /// </summary>
    public partial class ConstantBuffer : Node, IAttributes
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ConstantBuffer" /> class.
        /// </summary>
        public ConstantBuffer()
        {
            Members = new List<Node>();
            Attributes = new List<AttributeBase>();
            Qualifiers = Qualifier.None;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the attributes.
        /// </summary>
        /// <value>
        ///   The attributes.
        /// </value>
        public List<AttributeBase> Attributes { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether this instance is texture buffer.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is texture buffer; otherwise, <c>false</c>.
        /// </value>
        public ConstantBufferType Type { get; set; }

        /// <summary>
        ///   Gets or sets the members.
        /// </summary>
        /// <value>
        ///   The members.
        /// </value>
        public List<Node> Members { get; set; }

        /// <summary>
        ///   Gets or sets the name.
        /// </summary>
        /// <value>
        ///   The name.
        /// </value>
        public Identifier Name { get; set; }

        /// <summary>
        ///   Gets or sets the register.
        /// </summary>
        /// <value>
        ///   The register.
        /// </value>
        public RegisterLocation Register { get; set; }

        /// <summary>
        /// Gets or sets the qualifiers.
        /// </summary>
        /// <value>
        /// The qualifiers.
        /// </value>
        public Qualifier Qualifiers { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.AddRange(Attributes);
            ChildrenList.Add(Type);
            if (Name != null) ChildrenList.Add(Name);
            if (Register != null) ChildrenList.Add(Register);
            ChildrenList.AddRange(Members);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0} {1} {{...}}", Type, Name);
        }

        #endregion
    }
}
