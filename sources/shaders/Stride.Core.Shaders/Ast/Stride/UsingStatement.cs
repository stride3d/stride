// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Ast.Stride
{
    /// <summary>
    /// A using statement.
    /// </summary>
    public partial class UsingStatement : Statement
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "UsingStatement" /> class.
        /// </summary>
        public UsingStatement()
        {
        }

        public Identifier Name;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Name);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("using {0}",  Name);
        }

        #endregion
    }
}
