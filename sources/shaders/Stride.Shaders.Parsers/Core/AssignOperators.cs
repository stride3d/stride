namespace Stride.Shaders.Core;

public enum AssignOperator
{
    NOp,
    Simple,
    Plus,
    Minus,
    Mul,
    Div,
    Mod,
    RightShift,
    LeftShift,
    AND,
    OR,
    XOR
}


public static class StringAssignOperatorExtensions
{
    public static AssignOperator ToAssignOperator(this ReadOnlySpan<char> s)
    {
        return s switch
        {
            "=" => AssignOperator.Simple,
            "+=" => AssignOperator.Plus,
            "-=" => AssignOperator.Minus,
            "*=" => AssignOperator.Mul,
            "/=" => AssignOperator.Div,
            "%=" => AssignOperator.Mod,
            ">>=" => AssignOperator.RightShift,
            "<<=" => AssignOperator.LeftShift,
            "&=" => AssignOperator.AND,
            "|=" => AssignOperator.OR,
            "^=" => AssignOperator.XOR,
            _ => AssignOperator.NOp
        };
    }
    public static AssignOperator ToAssignOperator(this string s)
    {
        return s switch
        {
            "=" => AssignOperator.Simple,
            "+=" => AssignOperator.Plus,
            "-=" => AssignOperator.Minus,
            "*=" => AssignOperator.Mul,
            "/=" => AssignOperator.Div,
            "%=" => AssignOperator.Mod,
            ">>=" => AssignOperator.RightShift,
            "<<=" => AssignOperator.LeftShift,
            "&=" => AssignOperator.AND,
            "|=" => AssignOperator.OR,
            "^=" => AssignOperator.XOR,
            _ => AssignOperator.NOp
        };
    }
    public static string ToAssignSymbol(this AssignOperator s)
    {
        return s switch
        {
            AssignOperator.Simple => "=",
            AssignOperator.Plus => "+=",
            AssignOperator.Minus => "-=",
            AssignOperator.Mul => "*=",
            AssignOperator.Div => "/=",
            AssignOperator.Mod => "%=",
            AssignOperator.RightShift => ">>=",
            AssignOperator.LeftShift => "<<=",
            AssignOperator.AND => "&=",
            AssignOperator.OR => "|=",
            AssignOperator.XOR => "^=",
            _ => "NOp"
        };
    }
}

