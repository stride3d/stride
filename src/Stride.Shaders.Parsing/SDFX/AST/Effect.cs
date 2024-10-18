using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX.AST;


public class ShaderEffect(TypeName name, TextLocation info) : ShaderDeclaration(info)
{
    public TypeName Name { get; set; } = name;
    public List<EffectStatement> Members { get; set; } = [];

    public override string ToString()
    {
        return string.Join("", Members.Select(x => $"{x}\n"));
    }
}

public abstract class EffectStatement(TextLocation info) : Node(info);

public class MixinUse(InheritedMixin mixin, TextLocation info) : EffectStatement(info)
{
    public InheritedMixin MixinName { get; set; } = mixin;
    public override string ToString()
    {
        return $"mixin {MixinName}";
    }
}

public abstract class Composable();

public class MixinCompose(Identifier identifier, InheritedMixin mixin, TextLocation info) : EffectStatement(info)
{
    public Identifier Identifier { get; set; } = identifier;
    public InheritedMixin MixinName { get; set; } = mixin;
    public override string ToString()
    {
        return $"mixin compose {Identifier} = {MixinName}";
    }
}

public class ComposeParams(InheritedMixin mixin, TextLocation info) : EffectStatement(info)
{
    public InheritedMixin MixinName { get; set; } = mixin;
}
public class UsingParams(Identifier name, TextLocation info) : EffectStatement(info)
{
    public Identifier ParamsName { get; set; } = name;

    public override string ToString()
    {
        return $"using params {ParamsName}";
    }
}

public class EffectBlock(TextLocation info) : EffectStatement(info)
{
    public List<EffectStatement> Statements { get; set; } = [];
}


