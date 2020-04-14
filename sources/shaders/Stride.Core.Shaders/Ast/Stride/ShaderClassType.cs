// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;

namespace Stride.Core.Shaders.Ast.Stride
{
    /// <summary>
    /// Shader Class.
    /// </summary>
    public partial class ShaderClassType : ClassType
    {
        // temp
        public List<Variable> ShaderGenerics { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassType"/> class.
        /// </summary>
        public ShaderClassType()
        {
            ShaderGenerics = new List<Variable>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassType"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ShaderClassType(string name) : base(name)
        {
            ShaderGenerics = new List<Variable>();
        }

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.AddRange(BaseClasses);
            ChildrenList.AddRange(GenericParameters);
            ChildrenList.AddRange(ShaderGenerics);
            ChildrenList.AddRange(Members);
            return ChildrenList;
        }
    }
}
