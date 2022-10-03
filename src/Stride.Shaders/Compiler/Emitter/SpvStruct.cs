using Spv.Generator;
using Stride.Shaders.Mixer;
using Stride.Shaders.Parsing.AST.Shader.Analysis;

namespace Stride.Shaders.Spirv;


public class SpvStruct
{
    public Instruction SpvType {get;set;}
    public CompositeType Definition {get;set;}

    public SpvStruct(Instruction spvType, CompositeType definition)
    {
        SpvType = spvType;
        Definition = definition;
    }
}

