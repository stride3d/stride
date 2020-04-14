// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Shaders.Ast.Stride
{
    /// <summary>
    /// Shader Class that supports adding mixin class to its base classes.
    /// </summary>
    public partial class ShaderRootClassType : ShaderClassType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderRootClassType"/> class.
        /// </summary>
        public ShaderRootClassType()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderRootClassType"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ShaderRootClassType(string name)
            : base(name)
        {
        }
    }
}
