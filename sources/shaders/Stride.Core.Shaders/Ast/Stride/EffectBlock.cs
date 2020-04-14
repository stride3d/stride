// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Ast.Stride
{
    /// <summary>
    /// A Shader block.
    /// </summary>
    public partial class EffectBlock : TypeBase, IScopeContainer
    {
        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating whether this instance is partial.
        /// </summary>
        /// <value><c>true</c> if this instance is partial; otherwise, <c>false</c>.</value>
        public bool IsPartial { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        /// <value>The body.</value>
        public BlockStatement Body { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Name);
            ChildrenList.Add(Body);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0}shader {1} {{...}}", IsPartial ? "partial " : string.Empty, Name);
        }

        #endregion
    }
}
