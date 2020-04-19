// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Keyword expression statement like continue; break; discard;
    /// </summary>
    public partial class KeywordExpression : Expression
    {
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordExpression"/> class.
        /// </summary>
        public KeywordExpression()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeywordExpression"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public KeywordExpression(Identifier name)
        {
            Name = name;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public Identifier Name { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Name);
            return ChildrenList;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
