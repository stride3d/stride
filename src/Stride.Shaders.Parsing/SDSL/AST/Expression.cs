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

public class PostfixExpression(Expression expression, Operator op, TextLocation info) : UnaryExpression(expression, op, info)
{
    public override string ToString()
    {
        return $"{Expression}{Operator.ToSymbol()}";
    }
}

public class AccessorExpression(Expression expression, Expression accessed, TextLocation info) : PostfixExpression(expression, Operator.Accessor, info)
{
    public Expression Accessed { get; set; } = accessed;

    public override string ToString()
    {
        return $"{Expression}.{Accessed}";
    }
}

public class IndexerExpression(Expression expression, Expression index, TextLocation info) : PostfixExpression(expression, Operator.Indexer, info)
{
    public Expression Index { get; set; } = index;
    public override string ToString()
    {
        return $"{Expression}[{Index}]";
    }
}


public class BinaryExpression(Expression left, Operator op, Expression right, TextLocation info) : Expression(info)
{
    public Operator Op { get; set; } = op;
    public Expression Left { get; set; } = left;
    public Expression Right { get; set; } = right;

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