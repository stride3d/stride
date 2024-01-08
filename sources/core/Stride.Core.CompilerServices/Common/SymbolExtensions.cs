namespace Stride.Core.CompilerServices.Common;
internal static class SymbolExtensions
{
    public static bool IsVisibleToSerializer(this ISymbol symbol, INamedTypeSymbol dataMemberAttribute)
    {
        if (symbol.HasAttribute(dataMemberAttribute))
            return IsVisibleToSerializer(symbol, hasDataMemberAttribute: true);
        return IsVisibleToSerializer(symbol, hasDataMemberAttribute: false);
    }

    public static bool IsVisibleToSerializer(this ISymbol symbol, bool hasDataMemberAttribute)
    {
        var accessibility = symbol.DeclaredAccessibility;

        if (hasDataMemberAttribute)
            return accessibility == Accessibility.Public || accessibility == Accessibility.Internal || accessibility == Accessibility.ProtectedOrInternal;
        return accessibility == Accessibility.Public;
    }

    /// <summary>
    /// An Immutable Type is treated if its a non Reference Type ie class
    /// A struct and a string are treated as Immutable as the Yaml Serializer can't handle value Types with it's reflection.
    /// </summary>
    /// <param name="type">The Type to check against</param>
    /// <returns>true when it's an Immutable Type</returns>
    public static bool IsImmutableType(this ITypeSymbol type)
    {
        return type.SpecialType == SpecialType.System_String || !type.IsReferenceType;
    }

    public static bool HasDataMemberMode(this ISymbol symbol, SymbolAnalysisContext context, INamedTypeSymbol dataMemberAttribute, INamedTypeSymbol dataMemberMode, int mode)
    {
        var attributes = symbol.GetAttributes();
        foreach (var attribute in attributes)
        {
            if (!attribute.AttributeClass?.Equals(dataMemberAttribute, SymbolEqualityComparer.Default) ?? false)
                continue;

            var modeParameter = attribute.ConstructorArguments.FirstOrDefault(x => x.Type?.Equals(dataMemberMode, SymbolEqualityComparer.Default) ?? false);

            if (modeParameter.Value is null)
                return false;
            if ((int)modeParameter.Value == mode)
            {
                return true;
            }
        }
        return false;
    }
}
