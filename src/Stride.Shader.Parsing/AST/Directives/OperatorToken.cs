using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Directives;

public enum OperatorToken
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

public static class OperatorTokenExtensions
{
    public static OperatorToken AsOperatorToken(this string s)
    {
        return s switch
        {
            "*" => OperatorToken.Mul,
            "/" => OperatorToken.Div,
            "%" => OperatorToken.Mod,
            "+" => OperatorToken.Plus,
            "-" => OperatorToken.Minus,
            "<<" => OperatorToken.LeftShift,
            ">>" => OperatorToken.RightShift,
            "|" => OperatorToken.Or,
            "&" => OperatorToken.And,
            "^" => OperatorToken.Xor,
            "<" => OperatorToken.Less,
            "<=" => OperatorToken.LessEqual,
            ">" => OperatorToken.Greater,
            ">=" => OperatorToken.GreaterEqual,
            "==" => OperatorToken.Equals,
            "!=" => OperatorToken.NotEquals,
            "&&" => OperatorToken.LogicalAnd,
            "||" => OperatorToken.LogicalOr,
            _ => throw new NotImplementedException()
        };
    }
}
