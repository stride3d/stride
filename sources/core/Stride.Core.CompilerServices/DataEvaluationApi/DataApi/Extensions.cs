using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.DataEvaluationApi.DataApi;

public static class Extensions
{
    public static bool HasInheritedDataContract(this ITypeSymbol typeDeclaration, INamedTypeSymbol attribute)
    {
        if (typeDeclaration == null) return false;
        var baseType = typeDeclaration;

        while (baseType != null)
        {
            if (baseType.HasAttribute(attribute))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }
    public static bool IsPartial(this TypeDeclarationSyntax typeDeclarationSyntax)
    {
        return typeDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
    }
}
