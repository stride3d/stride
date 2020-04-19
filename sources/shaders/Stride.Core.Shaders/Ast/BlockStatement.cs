// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A Block of statement.
    /// </summary>
    public partial class BlockStatement : Statement, IScopeContainer
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "BlockStatement" /> class.
        /// </summary>
        public BlockStatement()
        {
            Statements = new StatementList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockStatement"/> class.
        /// </summary>
        /// <param name="statements">The statements.</param>
        public BlockStatement(StatementList statements)
        {
            Statements = statements;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the statements.
        /// </summary>
        /// <value>
        ///   The statements.
        /// </value>
        public StatementList Statements { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            return Statements;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "{...}";
        }

        #endregion
    }
}
