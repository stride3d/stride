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
    private List<Instruction> input = new();
    private List<Instruction> output = new();

    public Dictionary<string,SpvStruct> ShaderTypes {get;set;}
    public Dictionary<string,Instruction> ShaderFunctionTypes {get;set;}
    public Dictionary<string,Instruction> Variables {get;set;} = new();
    public SortedList<string,int> Semantics {get;set;}
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
        SetMemoryModel(AddressingModel.Logical, MemoryModel.Simple);
    }
    public void CreateSemantics(ShaderProgram program)
    {
        Semantics = new(program.Body.OfType<ShaderVariableDeclaration>().Where(x => x.Semantic!=null).Select((x,i) => (x,i)).ToDictionary(v=> v.x.Semantic ?? "", v => v.i));
    }
    public void Construct(ShaderProgram program, EntryPoints entry)
    {
        
        Initialize(entry);
        // Create all user defined types
        CreateSemantics(program);
        CreateStructs(program);
        // Create stream in out
        foreach(var f in ShaderTypes["VS_STREAM_IN"].Definition.Fields)
        {
            var ptr = TypePointer(StorageClass.Input, GetSpvType(f.Value));
            var v = Variable(ptr,StorageClass.Input);
            Name(v,f.Key);
            input.Add(v);
            AddGlobalVariable(v);
        }
        foreach(var f in ShaderTypes["VS_STREAM_OUT"].Definition.Fields)
        {
            var ptr = TypePointer(StorageClass.Output, GetSpvType(f.Value));
            var v = Variable(ptr,StorageClass.Output);
            Name(v,f.Key);
            input.Add(v);
            AddGlobalVariable(v);
        }
        // Generate methods()
        

        // Generate main method

        MainMethod(entry,program);
    }
}
