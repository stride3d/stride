// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Ast.Hlsl;

namespace Xenko.Core.Shaders.Ast.Xenko
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
