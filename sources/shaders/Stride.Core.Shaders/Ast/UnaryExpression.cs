// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A unary expression.
    /// </summary>
    public partial class UnaryExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnaryExpression"/> class.
        /// </summary>
        public UnaryExpression()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnaryExpression"/> class.
        /// </summary>
        /// <param name="operator">The @operator.</param>
        /// <param name="expression">The expression.</param>
        public UnaryExpression(UnaryOperator @operator, Expression expression)
        {
            this.Operator = @operator;
            this.Expression = expression;
        }

        /// <summary>
        /// Gets or sets the operator.
        /// </summary>
        /// <value>
        /// The operator.
        /// </value>
        public UnaryOperator Operator { get; set; }

        /// <summary>
        /// Gets or sets the expression.
        /// </summary>
        /// <value>
        /// The expression.
        /// </value>
        public Expression Expression { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Expression);
            return ChildrenList;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var isPostFix = Operator == UnaryOperator.PostIncrement || Operator == UnaryOperator.PostDecrement;
            var left = isPostFix ? (object)Expression : Operator.ConvertToString();
            var right = isPostFix ? Operator.ConvertToString() : (object)Expression;
            return string.Format("{0}{1}", left, right);
        }
    }
}
