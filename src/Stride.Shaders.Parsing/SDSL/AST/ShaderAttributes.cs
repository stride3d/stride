namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class ShaderAttribute(TextLocation info) : Node(info);

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