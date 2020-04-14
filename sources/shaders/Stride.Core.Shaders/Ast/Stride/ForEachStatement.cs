// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Ast.Stride
{
    /// <summary>
    /// For statement.
    /// </summary>
    public partial class ForEachStatement : Statement, IScopeContainer
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ForStatement" /> class.
        /// </summary>
        public ForEachStatement()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ForEachStatement"/> class.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="collection">The collection.</param>
        public ForEachStatement(Variable variable, Expression collection)
        {
            Variable = variable;
            Collection = collection;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the condition.
        /// </summary>
        /// <value>
        ///   The condition.
        /// </value>
        public Expression Collection { get; set; }

        /// <summary>
        ///   Gets or sets the initializer.
        /// </summary>
        /// <value>
        ///   The initializer.
        /// </value>
        public Variable Variable { get; set; }

        /// <summary>
        ///   Gets or sets the condition.
        /// </summary>
        /// <value>
        ///   The condition.
        /// </value>
        public Statement Body { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Collection);
            ChildrenList.Add(Variable);
            ChildrenList.Add(Body);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("foreach({0} in {1}) {{...}}", Variable, Collection);
        }

        #endregion
    }
}
