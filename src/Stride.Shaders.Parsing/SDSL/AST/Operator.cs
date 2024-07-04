namespace Stride.Shaders.Parsing.SDSL;

public enum Operator
{
    Nop,
    Cast,
    Positive,
    Negative,
    Not,
    /// <summary>
    /// Bitwise not
    /// </summary>
    BitwiseNot,
    /// <summary>
    /// Increment
    /// </summary>
    Inc,
    /// <summary>
    /// Decrement
    /// </summary>
    Dec,
    Plus,
    Minus,
    Mul,
    Div,
    Mod,
    RightShift,
    LeftShift,
    AND,
    OR,
    XOR,
    Greater,
    Lower,
    GreaterOrEqual,
    LowerOrEqual,
    NotEquals,
    Equals,
    LogicalAND,
    LogicalOR,
    Accessor,
    Indexer
}

public static class StringOperatorExtensions
{
    public static Operator ToOperator(this ReadOnlySpan<char> s)
    {
        return s switch
        {
            "!" => Operator.Not,
            "~" => Operator.BitwiseNot,
            "++" => Operator.Inc,
            "--" => Operator.Dec,
            "+" => Operator.Plus,
            "-" => Operator.Minus,
            "*" => Operator.Mul,
            "/" => Operator.Div,
            "%" => Operator.Mod,
            ">>" => Operator.RightShift,
            "<<" => Operator.LeftShift,
            "&" => Operator.AND,
            "|" => Operator.OR,
            "^" => Operator.XOR,
            ">" => Operator.Greater,
            "<" => Operator.Lower,
            ">=" => Operator.GreaterOrEqual,
            "<=" => Operator.LowerOrEqual,
            "==" => Operator.Equals,
            "!=" => Operator.NotEquals,
            "&&" => Operator.LogicalAND,
            "||" => Operator.LogicalOR,
            _ => Operator.Nop,
        };
    }
    public static Operator ToOperator(this string s)
    {
        return s switch
        {
            "!" => Operator.Not,
            "~" => Operator.BitwiseNot,
            "++" => Operator.Inc,
            "--" => Operator.Dec,
            "+" => Operator.Plus,
            "-" => Operator.Minus,
            "*" => Operator.Mul,
            "/" => Operator.Div,
            "%" => Operator.Mod,
            ">>" => Operator.RightShift,
            "<<" => Operator.LeftShift,
            "&" => Operator.AND,
            "|" => Operator.OR,
            "^" => Operator.XOR,
            ">" => Operator.Greater,
            "<" => Operator.Lower,
            ">=" => Operator.GreaterOrEqual,
            "<=" => Operator.LowerOrEqual,
            "==" => Operator.Equals,
            "!=" => Operator.NotEquals,
            "&&" => Operator.LogicalAND,
            "||" => Operator.LogicalOR,
            _ => Operator.Nop,
        };
    }
    public static string ToSymbol(this Operator s)
    {
        return s switch
        {
            Operator.Not => "!",
            Operator.BitwiseNot => "~",
            Operator.Inc => "++",
            Operator.Dec => "--",
            Operator.Plus => "+",
            Operator.Minus => "-",
            Operator.Mul => "*",
            Operator.Div => "/",
            Operator.Mod => "%",
            Operator.RightShift => ">>",
            Operator.LeftShift => "<<",
            Operator.AND => "&",
            Operator.OR => "|",
            Operator.XOR => "^",
            Operator.Greater => ">",
            Operator.Lower => "<",
            Operator.GreaterOrEqual => ">=",
            Operator.LowerOrEqual => "<=",
            Operator.Equals => "==",
            Operator.NotEquals => "!=",
            Operator.LogicalAND => "&&",
            Operator.LogicalOR => "||",
            _ => "NOp"
        };
    }

    public static Operator ToOperator(this char c)
    {
        return c switch
        {
            '!' => Operator.Not,
            '~' => Operator.BitwiseNot,
            '+' => Operator.Plus,
            '-' => Operator.Minus,
            '*' => Operator.Mul,
            '/' => Operator.Div,
            '%' => Operator.Mod,
            '&' => Operator.AND,
            '|' => Operator.OR,
            '^' => Operator.XOR,
            '>' => Operator.Greater,
            '<' => Operator.Lower,
            _ => Operator.Nop,
        };
    }
}
