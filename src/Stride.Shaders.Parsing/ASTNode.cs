using System.Text;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing;

public abstract class Node(TextLocation info)
{
    public TextLocation Info { get; set; } = info;
    public virtual void ProcessSymbol(SymbolTable table) => throw new NotImplementedException($"Symbol table cannot process type : {GetType().Name}");
}
public class ValueNode(TextLocation info) : Node(info)
{
    public virtual SymbolType? Type { get; set; } = null;
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

    public override void ProcessSymbol(SymbolTable table)
    {
        foreach (var e in RootDeclarations)
            e.ProcessSymbol(table);
        foreach (var ns in Namespaces)
            ns.ProcessSymbol(table);
    }

    public override string ToString()
    {
        return $"{string.Join("\n", RootDeclarations)}\n\n{string.Join("\n", Namespaces)}";
    }
}

public class UsingShaderNamespace(TextLocation info) : ShaderDeclaration(info)
{
    public List<Identifier> NamespacePath { get; set; } = [];
}

public class ShaderNamespace(TextLocation info) : Node(info)
{
    public List<Identifier> NamespacePath { get; set; } = [];
    public Identifier? Namespace { get; set; }
    public List<ShaderDeclaration> Declarations { get; set; } = [];

    public override void ProcessSymbol(SymbolTable table)
    {
        foreach(var d in Declarations)
            d.ProcessSymbol(table);
    }

    public override string ToString()
    {
        return $"namespace {string.Join(".", NamespacePath)}\nBlock\n{string.Join("\n", Declarations)}End\n";
    }
}