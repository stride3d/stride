namespace Stride.Shaders.ThreeAddress;

public enum AssignOperator : byte
{
    Equal,
    MulEqual,
    DivEqual,
    ModEqual,
    PlusEqual,
    MinusEqual,
    LeftShiftEqual,
    RightShiftEqual,
    AndEqual,
    OrEqual,
    XorEqual
}

public enum Operator : byte
{
    Mul,
    Div,
    Mod,
    Plus,
    Minus,
    LeftShift,
    RightShift,
    And,
    Or,
    Xor,
    Less,
    Greater,
    LessEqual,
    GreaterEqual,
    Equals,
    NotEquals,
    LogicalAnd,
    LogicalOr
}