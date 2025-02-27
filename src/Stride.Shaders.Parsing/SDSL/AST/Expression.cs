using System.Text;
using Stride.Shaders.Parsing.Analysis;

namespace Stride.Shaders.Parsing.SDSL.AST;




public abstract class Expression(TextLocation info) : ValueNode(info);

public class MethodCall(Identifier name, ShaderExpressionList parameters, TextLocation info) : Expression(info)
{
    public Identifier Name = name;
    public ShaderExpressionList Parameters = parameters;

    public override string ToString()
    {
        return $"{Name}({string.Join(", ", Parameters)})";
    }
}

public class MixinAccess(Mixin mixin, TextLocation info) : Expression(info)
{
    public Mixin Mixin { get; set; } = mixin;
    public override string ToString()
    {
        return $"{Mixin}";
    }
}


public abstract class UnaryExpression(Expression expression, Operator op, TextLocation info) : Expression(info)
{
    public Expression Expression { get; set; } = expression;
    public Operator Operator { get; set; } = op;
}

public class PrefixExpression(Operator op, Expression expression, TextLocation info) : UnaryExpression(expression, op, info);

public class CastExpression(string typeName, Operator op, Expression expression, TextLocation info) : PrefixExpression(op, expression, info)
{
    public string TypeName { get; set; } = typeName;
}

public class PostfixIncrement(Operator op, TextLocation info) : Expression(info)
{
    public Operator Operator { get; set; } = op;
    public override string ToString()
    {
        return $"{Operator.ToSymbol()}";
    }
}

public class AccessorChainExpression(Expression source, TextLocation info) : Expression(info)
{
    public Expression Source { get; set; } = source;
    public List<Expression> Accessors { get; set; } = [];

    public override string ToString()
    {
        var builder = new StringBuilder().Append(Source);
        foreach(var a in Accessors)
            if(a is NumberLiteral)
                builder.Append('[').Append(a).Append(']');
            else if(a is PostfixIncrement)
                builder.Append(a);
            else
                builder.Append('.').Append(a);
        return builder.ToString();
    }
}

public class BinaryExpression(Expression left, Operator op, Expression right, TextLocation info) : Expression(info)
{
    public Operator Op { get; set; } = op;
    public Expression Left { get; set; } = left;
    public Expression Right { get; set; } = right;

    public override void ProcessSymbol(SymbolTable table)
    {
        Left.ProcessSymbol(table);
        Right.ProcessSymbol(table);
        if (
            OperatorTable.BinaryOperationResultingType(
                Left.Type ?? throw new NotImplementedException("Missing type"),
                Right.Type ?? throw new NotImplementedException("Missing type"),
                Op,
                out var t
            )
        )
            Type = t;
        else
            table.Errors.Add(new(Info, SDSLErrorMessages.SDSL0104));
    }

    public override string ToString()
    {
        return $"( {Left} {Op.ToSymbol()} {Right} )";
    }
}

public class TernaryExpression(Expression cond, Expression left, Expression right, TextLocation info) : Expression(info)
{
    public Expression Condition { get; set; } = cond;
    public Expression Left { get; set; } = left;
    public Expression Right { get; set; } = right;

    public override string ToString()
    {
        return $"({Condition} ? {Left} : {Right})";
    }
}