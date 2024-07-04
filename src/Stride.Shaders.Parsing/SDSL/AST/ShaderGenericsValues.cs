namespace Stride.Shaders.Parsing.SDSL.AST;


public interface IGenericValue;
public abstract class ShaderGenericsValue(TextLocation info) : Node(info);


public class ValueTypeGenerics(ValueLiteral value,TextLocation info) : ShaderGenericsValue(info)
{
    public ValueLiteral Value { get; set; } = value;
}

public class IdentifierGenerics(Identifier identifier, TextLocation info) : ShaderGenericsValue(info)
{
    public Identifier Identifier { get; set; } = identifier;
}
public class AccessorExpressionGenerics(AccessorExpression accessor, TextLocation info) : ShaderGenericsValue(info)
{
    public AccessorExpression Accessor { get; set; } = accessor.Accessed is Identifier ? accessor : throw new ArgumentException($"Value accessed should be a shader class or a variable name");
}