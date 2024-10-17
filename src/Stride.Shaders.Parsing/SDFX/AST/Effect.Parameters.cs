using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX.AST;


public class EffectParameters(TypeName name, TextLocation info) : ShaderDeclaration(info)
{
    public TypeName Name { get; set; } = name;
    public List<EffectParameter> Parameters { get; set; } = [];
}


public class EffectParameter(TypeName type, Identifier identifier, TextLocation info, Expression? value = null) : Node(info)
{
    public TypeName Type { get; set; } = type;
    public Identifier Identifier { get; set;} = identifier;
    public Expression? DefaultValue { get; set; } = value;
}