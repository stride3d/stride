// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A Compile expression.
    /// </summary>
    public partial class CompileExpression : Expression
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CompileExpression" /> class.
        /// </summary>
        public CompileExpression()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompileExpression"/> class.
        /// </summary>
        /// <param name="profile">
        /// The profile.
        /// </param>
        /// <param name="function">
        /// The function.
        /// </param>
        public CompileExpression(string profile, MethodInvocationExpression function)
        {
            Profile = new Identifier(profile);
            Function = function;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///   Gets or sets the function.
        /// </summary>
        /// <value>
        ///   The function.
        /// </value>
        public Expression Function { get; set; }

        /// <summary>
        ///   Gets or sets the profile.
        /// </summary>
        /// <value>
        ///   The profile.
        /// </value>
        public Identifier Profile { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Profile);
            ChildrenList.Add(Function);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("compile {0} {1}", Profile, Function);
        }

        #endregion
    }
}
