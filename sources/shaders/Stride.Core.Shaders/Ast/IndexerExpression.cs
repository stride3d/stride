// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Indexer expression.
    /// </summary>
    public partial class IndexerExpression : Expression
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "IndexerExpression" /> class.
        /// </summary>
        public IndexerExpression()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexerExpression"/> class.
        /// </summary>
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="index">
        /// The index.
        /// </param>
        public IndexerExpression(Expression target, Expression index)
        {
            this.Target = target;
            this.Index = index;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the index.
        /// </summary>
        /// <value>
        ///   The index.
        /// </value>
        public Expression Index { get; set; }

        /// <summary>
        ///   Gets or sets the target.
        /// </summary>
        /// <value>
        ///   The target.
        /// </value>
        public Expression Target { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Target);
            ChildrenList.Add(Index);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0}[{1}]", Target, Index);
        }

        #endregion
    }
}
