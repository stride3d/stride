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
    public Dictionary<string,SpvStruct> ShaderTypes {get;set;}
    public Dictionary<string,Instruction> ShaderFunctionTypes {get;set;}
    public Dictionary<string,Instruction> Variables {get;set;} = new();
    public SpirvEmitter(uint version) : base(version)
    {
        ShaderTypes = new();
        CreateStructTypes();
    }

    public void Initialize(EntryPoints entry)
    {
        var capability = entry switch
        {
            EntryPoints.GSMain => Capability.Geometry,
            _ => Capability.Shader,
        };
        AddCapability(capability);
        SetMemoryModel(AddressingModel.Logical, MemoryModel.Simple);
    }

    public void Construct(ShaderProgram program, EntryPoints entry)
    {
        
        Initialize(entry);
        // Create all user defined types

        // Create stream types

        CreateStreamStructs(program, entry);

        // Generate methods()
        

        // Generate main method

        MainMethod(entry,program);
    }
}
