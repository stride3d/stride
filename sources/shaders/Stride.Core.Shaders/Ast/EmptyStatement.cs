// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A Empty of statement.
    /// </summary>
    public partial class EmptyStatement : Statement
    {
        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the <see cref = "EmptyStatement" /> class.
        /// </summary>
        public EmptyStatement()
        {
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Empty;
        }
        #endregion
    }
}
