// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Shaders.Ast.Xenko;
using Xenko.Core.Shaders.Ast;

namespace Xenko.Shaders.Parser.Mixins
{
    [DataContract]
    internal class VariableShaderCouple
    {
        public Variable Variable;
        public ShaderClassType Shader;

        public VariableShaderCouple() : this(null, null) { }
        
        public VariableShaderCouple(Variable variable, ShaderClassType shader)
        {
            Variable = variable;
            Shader = shader;
        }
    }
}
