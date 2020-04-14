// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Shaders.Ast.Xenko;
using Xenko.Core.Shaders.Ast;

namespace Xenko.Shaders.Parser.Mixins
{
    [DataContract]
    internal class MethodDeclarationShaderCouple
    {
        public MethodDeclaration Method;
        public ShaderClassType Shader;

        public MethodDeclarationShaderCouple() : this(null, null){}

        public MethodDeclarationShaderCouple(MethodDeclaration method, ShaderClassType shader)
        {
            Method = method;
            Shader = shader;
        }
    }
}
