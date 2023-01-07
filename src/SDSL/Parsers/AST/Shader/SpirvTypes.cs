using Spv.Generator;
using static Spv.Generator.Instruction;


namespace SDSL.Parsing.AST.Shader;



public static class SpirvTypes
{
    public static Instruction GetSpirvType(string name)
    {
        return name switch
        {
            // "float" => TypeFloat()
            _ => throw new NotImplementedException()
        };
    }
}