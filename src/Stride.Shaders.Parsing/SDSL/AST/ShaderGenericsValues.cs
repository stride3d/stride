namespace Stride.Shaders.Parsing.SDSL.AST;


public interface IGenericValue;
public abstract class ShaderGenericsValue(TextLocation info) : Node(info);


public class ValueTypeGenerics(ValueLiteral value,TextLocation info) : ShaderGenericsValue(info)
{
    public ValueLiteral Value { get; set; } = value;
}