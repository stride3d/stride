// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A cast expression.
    /// </summary>
    public partial class CastExpression : Expression
    {
        #region Public Properties

        /// <summary>
        ///   Gets or sets from.
        /// </summary>
        /// <value>
        ///   From.
        /// </value>
        public Expression From { get; set; }

        /// <summary>
        ///   Gets or sets the target.
        /// </summary>
        /// <value>
        ///   The target.
        /// </value>
        public TypeBase Target { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Target);
            ChildrenList.Add(From);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("({0}){1}", Target, From);
        }

        #endregion
    }
}
