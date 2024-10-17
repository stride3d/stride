using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDFX.AST;


public class ShaderEffect(TypeName name, TextLocation info) : ShaderDeclaration(info)
{
    public TypeName Name { get; set; } = name;
    public List<EffectStatement> Members { get; set; } = [];
}

public abstract class EffectStatement(TextLocation info) : Node(info);

public class MixinUse(InheritedMixin mixin, TextLocation info) : EffectStatement(info)
{
    public InheritedMixin MixinName { get; set; } = mixin;
}

public abstract class Composable();

public class MixinCompose(InheritedMixin mixin, TextLocation info) : EffectStatement(info)
{
    public InheritedMixin MixinName { get; set; } = mixin;
}

public class ComposeParams(InheritedMixin mixin, TextLocation info) : EffectStatement(info)
{
    public InheritedMixin MixinName { get; set; } = mixin;
}
public class UsingParams(Identifier name, TextLocation info) : EffectStatement(info)
{
    public Identifier ParamsName { get; set; } = name;
}

public class EffectBlock(TextLocation info) : EffectStatement(info)
{
    public List<EffectStatement> Statements { get; set; } = [];
}


