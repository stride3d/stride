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
    public Dictionary<string,Instruction> ShaderTypes {get;set;}

    public SpirvEmitter(uint version) : base(version)
    {
        ShaderTypes = new();
        CreateNativeTypes();
    }

    public void Construct(ShaderProgram program, EntryPoints entry)
    {
        AddCapability(Capability.Shader);
        SetMemoryModel(AddressingModel.Logical, MemoryModel.Simple);
        
        // Create all user defined types

        // Create stream types

        CreateStreamStructs(program, entry);

        // Manage input output and stream

        // Generate methods()
        
    }
}
