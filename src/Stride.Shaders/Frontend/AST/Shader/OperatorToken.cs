using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;

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

    private static double OperationWithCast(object a, object b, Func<double,double,double> f)
    {
        var l = Convert.ToDouble(a);
        var r = Convert.ToDouble(b);
        return f.Invoke(l, r);
    }
    private static double OperationWithCast(object a, object b, Func<int, int, double> f)
    {
        var l = Convert.ToInt32(a);
        var r = Convert.ToInt32(b);
        return f.Invoke(l, r);
    }

    private static bool OperationWithCast(object a, object b, Func<double, double, bool> f)
    {
        var l = Convert.ToDouble(a);
        var r = Convert.ToDouble(b);
        return f.Invoke(l, r);
    }

    public static ShaderToken ApplyOperation(OperatorToken op, NumberLiteral l, NumberLiteral r)
    {
        return op switch
        {
            OperatorToken.Mul => new NumberLiteral { Value = OperationWithCast(l.Value,r.Value ,(a,b) => a*b) },
            OperatorToken.Div => new NumberLiteral { Value = OperationWithCast(l.Value, r.Value , (a, b) => a / b) },
            OperatorToken.Mod => new NumberLiteral { Value = OperationWithCast(l.Value, r.Value , (a, b) => a % b) },
            OperatorToken.Plus => new NumberLiteral { Value = OperationWithCast(l.Value, r.Value , (a, b) => a + b) },
            OperatorToken.Minus => new NumberLiteral { Value = OperationWithCast(l.Value, r.Value , (a, b) => a - b) },
            OperatorToken.LeftShift => new NumberLiteral { Value = OperationWithCast(l.Value, r.Value , (a,b) => a << b) },
            OperatorToken.RightShift => new NumberLiteral { Value = OperationWithCast(l.Value, r.Value , (a, b) => a >> b) },
            OperatorToken.And => new NumberLiteral { Value = OperationWithCast(l.Value, r.Value , (a, b) => a & b)  },
            OperatorToken.Or => new NumberLiteral { Value = OperationWithCast(l.Value,r.Value ,(a,b) => a | b) },
            OperatorToken.Xor => new NumberLiteral { Value = OperationWithCast(l.Value,r.Value ,(a,b) => a ^ b) },
            OperatorToken.Less => new BoolLiteral { Value = OperationWithCast(l.Value,r.Value ,(a,b) => a < b) },
            OperatorToken.LessEqual => new BoolLiteral { Value = OperationWithCast(l.Value,r.Value ,(a,b) => a <= b) },
            OperatorToken.Greater => new BoolLiteral { Value = OperationWithCast(l.Value,r.Value ,(a,b) => a > b) },
            OperatorToken.GreaterEqual => new BoolLiteral { Value = OperationWithCast(l.Value,r.Value ,(a,b) => a >= b) },
            OperatorToken.Equals => new BoolLiteral { Value = OperationWithCast(l.Value,r.Value ,(a,b) => a == b) },
            OperatorToken.NotEquals => new BoolLiteral { Value = OperationWithCast(l.Value,r.Value ,(a,b) => a != b) },
            _ => throw new NotImplementedException()
        };
    }
    public static ShaderToken ApplyOperation(OperatorToken op, BoolLiteral l, BoolLiteral r)
    {
        return op switch
        {
            OperatorToken.LogicalAnd => new BoolLiteral { Value = l.Value && r.Value },
            OperatorToken.LogicalOr => new BoolLiteral { Value = l.Value || r.Value },
            OperatorToken.Equals => new BoolLiteral { Value = l.Value == r.Value },
            OperatorToken.NotEquals => new BoolLiteral { Value = l.Value != r.Value },
            _ => throw new NotImplementedException()
        };
    }
    public static ShaderToken ApplyOperation(OperatorToken op, StringLiteral l, StringLiteral r)
    {
        return op switch
        {
            OperatorToken.Equals => new BoolLiteral { Value = l.Value == r.Value },
            OperatorToken.NotEquals => new BoolLiteral { Value = l.Value != r.Value },
            _ => throw new NotImplementedException()
        };
    }
}
