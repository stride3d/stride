using System.Text;

namespace Stride.Shaders.Parsing;

public abstract class Node(TextLocation info)
{
    public TextLocation Info { get; set; } = info;
}
public class ValueNode(TextLocation info) : Node(info)
{
    public string? Type { get; set; } = null;
}
public class NoNode() : Node(new());

public class ListNode(TextLocation info) : Node(info)
{
    public List<Node> Nodes { get; set; } = [];
}

public abstract class ShaderDeclaration(TextLocation info) : Node(info);


public class ShaderFile(TextLocation info) : Node(info)
{
    public List<ShaderDeclaration> RootDeclarations { get; set; } = [];
    public List<ShaderNamespace> Namespaces { get; set; } = [];

    public override string ToString()
    {
        return $"{string.Join("\n", RootDeclarations)}\n\n{string.Join("\n", Namespaces)}";
    }
}

public class UsingShaderNamespace(TextLocation info) : ShaderDeclaration(info)
{
    public List<SDSL.AST.Identifier> NamespacePath { get; set; } = [];
}

public class ShaderNamespace(TextLocation info) : Node(info)
{
    public List<SDSL.AST.Identifier> NamespacePath { get; set; } = [];
    public string? Namespace { get; set; }
    public List<ShaderDeclaration> Declarations { get; set; } = [];

    public override string ToString()
    {
        return $"namespace {string.Join(".", NamespacePath)}\nBlock\n{string.Join("\n", Declarations)}End\n";
    }
}