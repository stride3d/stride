using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Core.StrideDiagnostics;

public class ClassInfo
{
    public GeneratorExecutionContext ExecutionContext { get; set; }
    public TypeDeclarationSyntax TypeSyntax { get; set; }
    public ClassDeclarationSyntax SerializerSyntax { get; set; }
    public TypeParameterListSyntax Generics => TypeSyntax.TypeParameterList;
    public bool IsGeneric => TypeSyntax.TypeParameterList != null && TypeSyntax.TypeParameterList.Parameters.Count > 0;
    public INamedTypeSymbol Symbol { get; set; }
    public string TypeName { get; set; }
    public string SerializerName { get; set; }
    public string NamespaceName { get; set; }
    public NexSyntaxReceiver SyntaxReceiver { get; set; }
    public SemanticModel SemanticModel { get => _compilationCache ??= ExecutionContext.Compilation.GetSemanticModel(TypeSyntax.SyntaxTree); }
    private SemanticModel _compilationCache;
    public bool IsAbstract
    {
        get
        {
            return TypeSyntax.Modifiers.Any(x => x.IsKind(SyntaxKind.AbstractKeyword));
        }
    }

}