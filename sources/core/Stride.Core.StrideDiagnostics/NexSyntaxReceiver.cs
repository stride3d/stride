using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Core.StrideDiagnostics;

public class NexSyntaxReceiver : ISyntaxReceiver
{
    public List<TypeDeclarationSyntax> TypeDeclarations { get; private set; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax typeSyntax)
        {
            TypeDeclarations.Add(typeSyntax);
        }
    }


}
