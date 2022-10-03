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
    private Instruction input;
    private Instruction output;

    public Dictionary<string,SpvStruct> ShaderTypes {get;set;}
    public Dictionary<string,Instruction> ShaderFunctionTypes {get;set;}
    public Dictionary<string,Instruction> Variables {get;set;} = new();
    public SpirvEmitter(uint version) : base(version)
    {
        ShaderTypes = new();
    }

    public void Initialize(EntryPoints entry)
    {
        var capability = entry switch
        {
            EntryPoints.GSMain => Capability.Geometry,
            _ => Capability.Shader,
        };
        AddCapability(capability);
        AddExtension("SPV_GOOGLE_decorate_string");
        AddExtension("SPV_GOOGLE_hlsl_functionality1");
        SetMemoryModel(AddressingModel.Logical, MemoryModel.Simple);
    }

    public void Construct(ShaderProgram program, EntryPoints entry)
    {
        
        Initialize(entry);
        // Create all user defined types
        CreateStructs(program);
        // Create stream in out
        // foreach(var f in ShaderTypes["VS_STREAM_IN"].Definition.Fields)
        // {
        //     var ptr = TypePointer(StorageClass.Input, GetSpvType(f.Value));
        //     var v = Variable(ptr,StorageClass.Input);
        //     Name(v,f.Key);
        //     input.Add(v);
        //     AddGlobalVariable(v);
        //     Decorate(v,Decoration.HlslSemanticGOOGLE, new LiteralString("POSITION"));
        // }
        // foreach(var f in ShaderTypes["VS_STREAM_OUT"].Definition.Fields)
        // {
        //     var ptr = TypePointer(StorageClass.Output, GetSpvType(f.Value));
        //     var v = Variable(ptr,StorageClass.Output);
        //     Name(v,f.Key);
        //     input.Add(v);
        //     AddGlobalVariable(v);
        // }
        var pInput = TypePointer(StorageClass.Input,ShaderTypes["VS_STREAM_IN"].SpvType);
        var pOutput = TypePointer(StorageClass.Output,ShaderTypes["VS_STREAM_OUT"].SpvType);
        input = Variable(pInput,StorageClass.Input);
        output = Variable(pOutput,StorageClass.Output);
        AddGlobalVariable(input);
        AddGlobalVariable(output);
        // Generate methods()
        

        // Generate main method

        MainMethod(entry,program);
    }
}
