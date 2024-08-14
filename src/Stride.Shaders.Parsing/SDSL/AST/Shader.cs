namespace Stride.Shaders.Parsing.SDSL.AST;



public class ShaderFile(TextLocation info) : Node(info)
{
    public List<ShaderClass> RootClasses { get; set; } = [];
    public List<ShaderNamespace> Namespaces { get; set; } = [];

    public override string ToString()
    {
        return $"{string.Join("\n", RootClasses)}\n\n{string.Join("\n", Namespaces)}";
    }
}

public class ShaderNamespace(TextLocation info) : Node(info)
{
    public List<Identifier> NamespacePath { get; set; } = [];
    public string? Namespace { get; set; }
    public List<ShaderClass> ShaderClasses { get; set; } = [];

    public override string ToString()
    {
        return $"namespace {string.Join(".", NamespacePath)}\nBlock\n{string.Join("\n", ShaderClasses)}End\n";
    }
}

public class ShaderClass(Identifier name, TextLocation info) : Node(info)
{
    public Identifier Name { get; set; } = name;
    public List<ShaderElement> Elements { get; set; } = [];
    public ShaderParameterDeclarations? Generics { get; set; }
    public List<ShaderMixin> Mixins { get; set; } = [];


    public override string ToString()
    {
        return
$"""
Class : {Name}
Generics : {string.Join(", ", Generics)}
Inherits from : {string.Join(", ", Mixins)}
Body :
{string.Join("\n", Elements)}
""";
    }
}


public class ShaderGenerics(Identifier typename, Identifier name, TextLocation info) : Node(info)
{
    public Identifier Name { get; set; } = name;
    public Identifier TypeName { get; set; } = typename;
}

public class ShaderMixin(Identifier name, TextLocation info) : Node(info)
{
    public Identifier Name { get; set; } = name;
    public ShaderExpressionList? Generics { get; set; }
    public override string ToString()
        => Generics switch
        {
            null => Name.Name,
            _ => $"{Name}<{Generics}>"
        };
}

public abstract class ShaderMixinValue(TextLocation info) : Node(info);
public class ShaderMixinExpression(Expression expression, TextLocation info) : ShaderMixinValue(info)
{
    public Expression Value { get; set; } = expression;
}
public class ShaderMixinIdentifier(Identifier identifier, TextLocation info) : ShaderMixinValue(info)
{
    public Identifier Value { get; set; } = identifier;
}