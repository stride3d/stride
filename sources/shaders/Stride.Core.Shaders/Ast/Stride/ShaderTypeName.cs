// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Ast.Stride
{
    /// <summary>
    /// A typeless reference.
    /// </summary>
    public partial class ShaderTypeName : TypeName
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderTypeName"/> class.
        /// </summary>
        public ShaderTypeName()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderTypeName"/> class.
        /// </summary>
        /// <param name="typeBase">The type base.</param>
        public ShaderTypeName(TypeBase typeBase)
            : base(typeBase.Name)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderTypeName"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ShaderTypeName(Identifier name) : base(name)
        {
        }
    }
}
