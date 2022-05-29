using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shader.Parsing.AST.Shader;

namespace Stride.Shader.Parsing;

public class SpirvEmitter : Module
{
    public SpirvEmitter(uint version) : base(version)
    {

    }
}
