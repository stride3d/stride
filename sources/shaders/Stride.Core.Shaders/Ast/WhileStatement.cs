// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// While and Do-While statement.
    /// </summary>
    public partial class WhileStatement : Statement, IScopeContainer
    {
        #region Public Properties

        /// <summary>
        ///   Gets or sets the condition.
        /// </summary>
        /// <value>
        ///   The condition.
        /// </value>
        public Expression Condition { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether this instance is a do while.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is a do while; otherwise, <c>false</c>.
        /// </value>
        public bool IsDoWhile { get; set; }

        /// <summary>
        ///   Gets or sets the statement.
        /// </summary>
        /// <value>
        ///   The statement.
        /// </value>
        public Statement Statement { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Condition);
            ChildrenList.Add(Statement);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (IsDoWhile)
            {
                return string.Format("do {{...}} while ({0})", Condition);
            }

            return string.Format("while ({0}) {{...}}", Condition);
        }

        #endregion
    }
}
