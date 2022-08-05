using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shaders.Mixer;
using Stride.Shaders.Parsing.AST.Shader;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;

public partial class SpirvEmitter : Module
{
    public void EmitStatement(Statement s, params Dictionary<string,Instruction>[] ScopedVariables)
    {
        if(s is AssignChain ac)
        {
            // TODO : Generate 3 addr

            // Convert 3 addr to spirv
        }
    }
}
