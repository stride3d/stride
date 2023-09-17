using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace StrideDiagnostics;

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
