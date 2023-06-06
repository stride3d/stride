using SDSL.Parsing.AST.Shader;

namespace SDSL.ThreeAddress;

public readonly struct SDArgument
{
    public readonly ShaderLiteral? Literal { get; }
    public readonly int? Location { get; }

    public bool IsLiteral => Literal != null;
    public bool IsLocation => Location != null;

    public SDArgument(ShaderLiteral lit)
    {
        Literal = lit;
    }
    public SDArgument(int loc)
    {
        Location = loc;
    }
}

public struct SDInstruction
{
    public TACOperator Op { get; set; }
    public SDArgument? Arg1 { get; set; }
    public SDArgument? Arg2 { get; set; }
    public SDArgument? Result { get; set; }

    public SDInstruction(TACOperator op, SDArgument? arg1 = null, SDArgument? arg2 = null, SDArgument? result = null)
    {
        Op = op;
        Arg1 = arg1;
        Arg2 = arg2;
        Result = result;
    }
}