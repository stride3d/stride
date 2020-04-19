// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// An expression statement.
    /// </summary>
    public partial class ExpressionStatement : Statement
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ExpressionStatement" /> class.
        /// </summary>
        public ExpressionStatement()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionStatement"/> class.
        /// </summary>
        /// <param name="expression">
        /// The expression.
        /// </param>
        public ExpressionStatement(Expression expression)
        {
            Expression = expression;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the expression.
        /// </summary>
        /// <value>
        ///   The expression.
        /// </value>
        public Expression Expression { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Expression);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0};", Expression);
        }

        #endregion
    }
}
