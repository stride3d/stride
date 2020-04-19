// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// If statement.
    /// </summary>
    public partial class IfStatement : Statement
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
        ///   Gets or sets the else.
        /// </summary>
        /// <value>
        ///   The else.
        /// </value>
        public Statement Else { get; set; }

        /// <summary>
        ///   Gets or sets the then.
        /// </summary>
        /// <value>
        ///   The then.
        /// </value>
        public Statement Then { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Condition);
            ChildrenList.Add(Then);
            ChildrenList.Add(Else);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("if ({0}) then {{...}}{1}", Condition, Else == null ? string.Empty : "...");
        }

        #endregion
    }
}
