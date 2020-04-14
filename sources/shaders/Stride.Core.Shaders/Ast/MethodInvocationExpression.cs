// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A method invocation.
    /// </summary>
    public partial class MethodInvocationExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodInvocationExpression"/> class.
        /// </summary>
        public MethodInvocationExpression()
        {
            Initialize(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodInvocationExpression"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="arguments">The arguments.</param>
        public MethodInvocationExpression(string name, params Expression[] arguments)
        {
            Initialize(new VariableReferenceExpression(name), arguments);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodInvocationExpression"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="arguments">The arguments.</param>
        public MethodInvocationExpression(Expression target, params Expression[] arguments)
        {
            Initialize(target, arguments);
        }

        private void Initialize(Expression target, params Expression[] arguments)
        {
            Target = target;
            Arguments = new List<Expression>();
            if (arguments != null)
                Arguments.AddRange(arguments);            
        }

        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>
        /// The target.
        /// </value>
        public Expression Target { get; set; }

        /// <summary>
        /// Gets or sets the arguments.
        /// </summary>
        /// <value>
        /// The arguments.
        /// </value>
        public List<Expression> Arguments { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.AddRange(Arguments);
            ChildrenList.Add(Target);
            return ChildrenList;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}({1})", Target, string.Join(",", Arguments));
        }
    }
}
