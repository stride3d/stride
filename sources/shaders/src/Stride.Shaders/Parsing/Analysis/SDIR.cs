using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.Analysis;


public enum IROp
{
    Nop,
    Positive, Negative, BitwiseComplement,
    Mul, Div, Mod,
    Add, Sub,
    LeftShift, RightShift,
    Greater, GreaterThan, Lower, LowerThan,
    Equals, NotEquals,
    BitwiseAND, BitwiseXOR, BitwiseOR,    
    LogicalAND, LogicalOR,
    
} 

public record struct QuadrupleArg(
    string Name,
    Statement Statement
);

public record struct Quadruple(
    IROp Op,
    QuadrupleArg Arg1,
    QuadrupleArg Arg2,
    QuadrupleArg Result

);