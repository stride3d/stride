// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Core.Shaders.Ast;

namespace Stride.Shaders.Parser.Mixins
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
