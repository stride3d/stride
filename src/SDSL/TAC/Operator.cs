using SDSL.Parsing.AST.Shader;
using static Spv.Specification;

namespace SDSL.TAC;


public enum Operator
{
    Nop,
    Declare,
    Access,
    If,
    Goto,
    Call,
    Load,
    PushParam,
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

public static class OperatorExtensions
{
    public static Operator Convert(this OperatorToken token) =>
        token switch
        {
            OperatorToken.Mul => Operator.Mul,
            OperatorToken.Div => Operator.Div,
            OperatorToken.Mod => Operator.Mod,
            OperatorToken.Plus => Operator.Plus,
            OperatorToken.Minus => Operator.Minus,
            OperatorToken.LeftShift => Operator.LeftShift,
            OperatorToken.RightShift => Operator.RightShift,
            OperatorToken.And => Operator.And,
            OperatorToken.Or => Operator.Or,
            OperatorToken.Xor => Operator.Xor,
            OperatorToken.Less => Operator.Less,
            OperatorToken.Greater => Operator.Greater,
            OperatorToken.LessEqual => Operator.LessEqual,
            OperatorToken.GreaterEqual => Operator.GreaterEqual,
            OperatorToken.Equals => Operator.Equals,
            OperatorToken.NotEquals => Operator.NotEquals,
            OperatorToken.LogicalAnd => Operator.LogicalAnd,
            OperatorToken.LogicalOr => Operator.LogicalOr,
            _ => throw new NotImplementedException(),
        };
}