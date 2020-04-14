// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Switch statement.
    /// </summary>
    public partial class SwitchStatement : Statement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchStatement"/> class.
        /// </summary>
        public SwitchStatement()
        {
            Groups = new List<SwitchCaseGroup>();
        }

        #region Public Properties

        /// <summary>
        ///   Gets or sets the condition.
        /// </summary>
        /// <value>
        ///   The condition.
        /// </value>
        public Expression Condition { get; set; }

        /// <summary>
        ///   Gets or sets the cases.
        /// </summary>
        /// <value>
        ///   The cases.
        /// </value>
        public List<SwitchCaseGroup> Groups { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Condition);
            ChildrenList.AddRange(Groups);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("switch ({0}) {{...}}", Condition);
        }

        #endregion
    }
}
