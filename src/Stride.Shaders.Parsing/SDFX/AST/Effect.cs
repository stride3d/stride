using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX.AST;


public class Effect(TextLocation info) : Node(info)
{
    public List<EffectStatement> Members { get; set; } = [];
}

public abstract class EffectStatement(TextLocation info) : Node(info);

public class MixinCompose(Identifier name, TextLocation info) : EffectStatement(info)
{
    public Identifier MixinName { get; set; } = name;
}

public class ComposeMixin(Identifier name, TextLocation info) : EffectStatement(info)
{
    public Identifier MixinName { get; set; } = name;
}
public class UsingParams(Identifier name, TextLocation info) : EffectStatement(info)
{
    public Identifier ParamsName { get; set; } = name;
}

public class EffectBlock(TextLocation info) : EffectStatement(info)
{
    public List<EffectStatement> Statements { get; set; } = [];
}


