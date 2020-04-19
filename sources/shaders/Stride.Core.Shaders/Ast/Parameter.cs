// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A single parameter declaration.
    /// </summary>
    public partial class Parameter : Variable
    {
        private MethodDeclaration declaringMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class.
        /// </summary>
        public Parameter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        /// <param name="initialValue">The initial value.</param>
        public Parameter(TypeBase type, string name = null, Expression initialValue = null)
            : base(type, name, initialValue)
        {
        }

        #region Public Properties

        /// <summary>
        ///   Gets or sets the declaring method.
        /// </summary>
        /// <value>
        ///   The declaring method.
        /// </value>
        [VisitorIgnore]
        public MethodDeclaration DeclaringMethod
        {
            get
            {
                return declaringMethod;
            }
            set
            {
                declaringMethod = value;
            }
        }

        #endregion
    }
}
