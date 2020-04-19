// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Defines a generic parameter type.
    /// </summary>
    public partial class GenericParameterType : TypeBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameterType"/> class.
        /// </summary>
        public GenericParameterType()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameterType"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public GenericParameterType(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameterType"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public GenericParameterType(Identifier name)
            : base(name)
        {
        }
    }
}
