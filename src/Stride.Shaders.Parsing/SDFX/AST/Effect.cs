namespace Stride.Shaders.Parsing.SDFX.AST;


public class EffectFile(TextLocation info) : Node(info)
{
    public List<EffectClass> RootClasses { get; set; } = [];
    public List<EffectNamespace> Namespaces { get; set; } = [];

    public override string ToString()
    {
        return $"{string.Join("\n", RootClasses)}\n\n{string.Join("\n", Namespaces)}";
    }
}

public class EffectNamespace(TextLocation info) : Node(info)
{
    public List<SDSL.AST.Identifier> NamespacePath { get; set; } = [];
    public string? Namespace { get; set; }
    public List<EffectClass> ShaderClasses { get; set; } = [];

    public override string ToString()
    {
        return $"namespace {string.Join(".", NamespacePath)}\nBlock\n{string.Join("\n", ShaderClasses)}End\n";
    }
}

public class EffectClass(TextLocation info) : Node(info)
{
    public List<EffectStatement> Members { get; set; } = [];
}

public abstract class EffectStatement(TextLocation info) : Node(info);

public class MixinCompose(SDSL.AST.Identifier name, TextLocation info) : EffectStatement(info)
{
    public SDSL.AST.Identifier MixinName { get; set; } = name;
}

public class ComposeMixin(SDSL.AST.Identifier name, TextLocation info) : EffectStatement(info)
{
    public SDSL.AST.Identifier MixinName { get; set; } = name;
}
public class UsingParams(SDSL.AST.Identifier name, TextLocation info) : EffectStatement(info)
{
    public SDSL.AST.Identifier ParamsName { get; set; } = name;
}

public class EffectBlock(TextLocation info) : EffectStatement(info)
{
    public List<EffectStatement> Statements { get; set; } = [];
}


