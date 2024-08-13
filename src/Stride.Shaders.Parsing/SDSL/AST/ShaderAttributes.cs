using System.Security.Cryptography;

namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class ShaderAttribute(TextLocation info) : Node(info);
public sealed class ShaderAttributeList(List<ShaderAttribute> attributes, TextLocation info) : Node(info)
{
    public List<ShaderAttribute> Attributes { get; } = attributes;
}

public class AnyShaderAttribute(Identifier name, TextLocation info, List<Expression> parameters = null!) : ShaderAttribute(info)
{
    public Identifier Name { get; set; } = name;
    public List<Expression> Parameters { get; } = parameters ?? [];

    public override string ToString()
    {
        return Parameters switch
        {
            null => Name.Name,
            _ => $"{Name}({string.Join(", ",Parameters.Select(x => x.ToString()))})"
        };
    }
}


public class ResourceBind(int location, int space, TextLocation info) : ShaderAttribute(info)
{
    public int Location { get; set; } = location;
    public int Space { get; set; } = space;

    public override string ToString()
    {
        return $"Bind({Location}, {Space})";
    }
}

public class ColorType(TextLocation info) : ShaderAttribute(info)
{
    public override string ToString() => "COLOR";
}
