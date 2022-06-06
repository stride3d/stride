using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shaders.Parsing.AST.Shader;
using static Spv.Specification;

namespace Stride.Shaders;

public partial class SpirvEmitter : Module
{
    public SpirvEmitter(uint version) : base(version)
    {

    }

    public void Construct(ShaderClassString code, EntryPoints entry)
    {
        AddCapability(Capability.Shader);
        SetMemoryModel(AddressingModel.Logical, MemoryModel.Simple);
        
        // Create all user defined types

        // Manage input output and stream

        // Generate methods
        
    }
}
