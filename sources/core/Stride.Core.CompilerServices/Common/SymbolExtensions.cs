namespace Stride.Core.CompilerServices.Common;
internal static class SymbolExtensions
{
    public static bool IsVisibleToSerializer(this ISymbol symbol, INamedTypeSymbol dataMemberAttribute)
    {
        var accessibility = symbol.DeclaredAccessibility;
        if(symbol.HasAttribute(dataMemberAttribute))
                return accessibility == Accessibility.Public || accessibility == Accessibility.Internal || accessibility == Accessibility.ProtectedOrInternal;
        return accessibility == Accessibility.Public;
    }
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
