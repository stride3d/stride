using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDSL.Parsing.AST.Shader;

public enum AssignOpToken
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
    public static OperatorToken ToOperatorToken(this string s)
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
    public static AssignOpToken ToAssignOp(this string s)
    {
        return s switch
        {
            "=" => AssignOpToken.Equal,
            "*=" => AssignOpToken.MulEqual,
            "/=" => AssignOpToken.DivEqual,
            "%=" => AssignOpToken.ModEqual,
            "+=" => AssignOpToken.PlusEqual,
            "-=" => AssignOpToken.MinusEqual,
            "<<=" => AssignOpToken.LeftShiftEqual,
            ">>=" => AssignOpToken.RightShiftEqual,
            "|=" => AssignOpToken.OrEqual,
            "&=" => AssignOpToken.AndEqual,
            "^=" => AssignOpToken.XorEqual,
            _ => throw new NotImplementedException()
        };
    }
}
