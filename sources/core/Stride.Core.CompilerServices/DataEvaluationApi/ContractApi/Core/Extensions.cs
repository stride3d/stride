using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Core;

public static class Extensionss
{
    public static bool HasAttribute(this ITypeSymbol symbol, INamedTypeSymbol attribute)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass?.OriginalDefinition.Equals(attribute, SymbolEqualityComparer.Default) ?? false))
            return true;
        return false;
    }
    public static bool TryGetAttribute(this ISymbol symbol, INamedTypeSymbol attribute, out AttributeData attributeData)
    {
        attributeData = symbol.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.OriginalDefinition.Equals(attribute, SymbolEqualityComparer.Default) ?? false);
        if (attributeData == null)
            return false;
        return true;
    }
    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass?.OriginalDefinition.Equals(attribute, SymbolEqualityComparer.Default) ?? false))
            return true;
        return false;
    }
    public static ITypeSymbol FindAttributeInInheritanceTree(this ITypeSymbol typeDeclaration, INamedTypeSymbol attribute)
    {
        if (typeDeclaration == null)
            return null;
        var baseType = typeDeclaration.BaseType;
        while (baseType != null)
        {
            if (baseType.HasAttribute(attribute))
                return baseType;
            baseType = baseType.BaseType;
        }
        return null;
    }
    public static string GetFullNamespace(this ITypeSymbol typeSymbol, char separator)
    {
        var namespaceSymbol = typeSymbol.ContainingNamespace;
        var fullNamespace = "";

        while (namespaceSymbol != null && !string.IsNullOrEmpty(namespaceSymbol.Name))
        {
            fullNamespace = namespaceSymbol.Name + separator + fullNamespace;
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        return fullNamespace.TrimEnd(separator);
    }
}
