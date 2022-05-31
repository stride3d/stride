using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shaders.Parsing.AST.Shader;

namespace Stride.Shaders.Parsing;

public class SpirvEmitter : Module
{
    public SpirvEmitter(uint version) : base(version)
    {

    }
}
