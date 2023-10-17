using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi;

public static class Extensions
{
    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass?.OriginalDefinition.Equals(attribute, SymbolEqualityComparer.Default) ?? false))
        {
            return true;
        }
        return false;
    }
    public static bool HasInheritedDataContractAttributeInInheritanceHierarchy(this ITypeSymbol typeDeclaration, INamedTypeSymbol attribute)
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
}
