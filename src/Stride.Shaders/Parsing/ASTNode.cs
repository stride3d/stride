using System.Text;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing;

/// <summary>
/// Base class for shader syntax tree elements
/// </summary>
public abstract class Node(TextLocation info)
{
    public TextLocation Info { get; set; } = info;
}

/// <summary>
/// AST Node with a type
/// </summary>
public class ValueNode(TextLocation info) : Node(info)
{
    public virtual SymbolType? Type { get; set; } = null;

    public virtual void ProcessType(SymbolTable table) => throw new NotImplementedException($"Symbol table cannot process type : {GetType().Name}");
}

/// <summary>
/// Empty node for empty result
/// </summary>
public class NoNode() : Node(new());


/// <summary>
/// A declaration in SDSL/SDFX
/// </summary>
public abstract class ShaderDeclaration(TextLocation info) : Node(info);


/// <summary>
/// Shader file node containing usings, namespaces, shaders, effects or params
/// </summary>
public class ShaderFile(TextLocation info) : Node(info)
{
    public List<ShaderDeclaration> RootDeclarations { get; set; } = [];
    public List<ShaderNamespace> Namespaces { get; set; } = [];

    public override string ToString()
    {
        return $"{string.Join("\n", RootDeclarations)}\n\n{string.Join("\n", Namespaces)}";
    }
}

/// <summary>
/// Using instructions
/// </summary>
public class UsingShaderNamespace(TextLocation info) : ShaderDeclaration(info)
{
    public List<Identifier> NamespacePath { get; set; } = [];
}

/// <summary>
/// Namespace declaration
/// </summary>
public class ShaderNamespace(TextLocation info) : Node(info)
{
    public List<Identifier> NamespacePath { get; set; } = [];
    public Identifier? Namespace { get; set; }
    public List<ShaderDeclaration> Declarations { get; set; } = [];

    public override string ToString()
    {
        return $"namespace {string.Join(".", NamespacePath)}\nBlock\n{string.Join("\n", Declarations)}End\n";
    }
}