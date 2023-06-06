using SDSL.Parsing.AST.Shader;

namespace SDSL.ThreeAddress;

public class IRSDSL
{
    List<SDInstruction> Instructions;


    public IRSDSL()
    {
        Instructions = new(32);
    }

    public int LowerToken(ShaderToken token)
    {
        return token switch
        {
            // BlockStatement t => Lower(t),
            // AssignChain t => Lower(t),
            DeclareAssign t => Lower(t),
            // ValueMethodCall t => Lower(t),
            // ConditionalExpression t => Lower(t),
            // Operation t => Lower(t),
            // ArrayAccessor t => Lower(t, isHead),
            // ChainAccessor t => Lower(t, isHead),
            VariableNameLiteral t => Lower(t),
            NumberLiteral t => Lower(t),
            _ => throw new NotImplementedException()
        };
    }

    int Lower(NumberLiteral lit)
    {
        Instructions.Add(new(TACOperator.Equal, new(lit),null, new(Instructions.Count)));
        return Instructions.Count - 1;
    }
    int Lower(VariableNameLiteral lit)
    {
        Instructions.Add(new(TACOperator.Equal, new(lit),null, new(Instructions.Count)));
        return Instructions.Count - 1;
    }
    int Lower(DeclareAssign token)
    {
        var valueLoc = LowerToken(token.Value);
        Instructions.Add(new(Convert(token.AssignOp),new(valueLoc),null,new(Instructions.Count)));
        return Instructions.Count - 1;
    }

    public static TACOperator Convert(OperatorToken op)
    {
        return op switch
        {
            OperatorToken.Plus => TACOperator.Plus,
            OperatorToken.Minus => TACOperator.Minus,
            OperatorToken.Mul => TACOperator.Mul,
            OperatorToken.Div => TACOperator.Div,
            OperatorToken.Mod => TACOperator.Mod,
            OperatorToken.Or => TACOperator.Or,
            OperatorToken.And => TACOperator.And,
            OperatorToken.Xor => TACOperator.Xor,
            OperatorToken.LogicalOr => TACOperator.LogicalOr,
            OperatorToken.LogicalAnd => TACOperator.LogicalAnd,
            OperatorToken.LeftShift => TACOperator.LeftShift,
            OperatorToken.RightShift => TACOperator.RightShift,
            OperatorToken.Less => TACOperator.Less,
            OperatorToken.Greater => TACOperator.Greater,
            OperatorToken.LessEqual => TACOperator.LessEqual,
            OperatorToken.GreaterEqual => TACOperator.GreaterEqual,
            OperatorToken.Equals => TACOperator.EqualEqual,
            OperatorToken.NotEquals => TACOperator.NotEqual,
            _ => throw new NotImplementedException()
        };
    }
    public static TACOperator Convert(AssignOpToken op)
    {
        return op switch
        {
            AssignOpToken.Equal => TACOperator.Equal,
            AssignOpToken.PlusEqual => TACOperator.PlusEqual,
            AssignOpToken.MinusEqual => TACOperator.MinusEqual,
            AssignOpToken.MulEqual => TACOperator.MulEqual,
            AssignOpToken.ModEqual => TACOperator.ModEqual,
            AssignOpToken.DivEqual => TACOperator.DivEqual,
            AssignOpToken.OrEqual => TACOperator.OrEqual,
            AssignOpToken.AndEqual => TACOperator.AndEqual,
            AssignOpToken.XorEqual => TACOperator.XorEqual,
            AssignOpToken.LeftShiftEqual => TACOperator.LeftShiftEqual,
            AssignOpToken.RightShiftEqual => TACOperator.RightShiftEqual,
            _ => throw new NotImplementedException()
        };
    }

}