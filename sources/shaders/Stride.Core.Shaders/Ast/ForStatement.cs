// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// For statement.
    /// </summary>
    public partial class ForStatement : Statement, IScopeContainer
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ForStatement" /> class.
        /// </summary>
        public ForStatement()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForStatement"/> class.
        /// </summary>
        /// <param name="start">
        /// The start.
        /// </param>
        /// <param name="condition">
        /// The condition.
        /// </param>
        /// <param name="next">
        /// The next.
        /// </param>
        public ForStatement(Statement start, Expression condition, Expression next)
        {
            Start = start;
            Condition = condition;
            Next = next;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the initializer.
        /// </summary>
        /// <value>
        ///   The initializer.
        /// </value>
        public Statement Start { get; set; }

        /// <summary>
        ///   Gets or sets the condition.
        /// </summary>
        /// <value>
        ///   The condition.
        /// </value>
        public Expression Condition { get; set; }

        /// <summary>
        ///   Gets or sets the next.
        /// </summary>
        /// <value>
        ///   The next.
        /// </value>
        public Expression Next { get; set; }

        /// <summary>
        ///   Gets or sets the body.
        /// </summary>
        /// <value>
        ///   The body.
        /// </value>
        public Statement Body { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Start);
            ChildrenList.Add(Condition);
            ChildrenList.Add(Next);
            ChildrenList.Add(Body);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("for({0}{1};{2}) {{...}}", Start, Condition, Next);
        }

        #endregion
    }
}
