using Stride.Shaders.Core;

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


public record struct SDID(
    string Name

);