namespace Stride.Core.CompilerServices.Common;
internal static class WellKnownReferences
{
    public static INamedTypeSymbol? DataMemberAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");
    }
    public static INamedTypeSymbol? IDictionary_generic(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName(typeof(IDictionary<,>).FullName);
    }
    public static INamedTypeSymbol? DataMemberIgnoreAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberIgnoreAttribute");
    }

    public static INamedTypeSymbol? DataMemberMode(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberMode");
    }

    public static INamedTypeSymbol? DataMemberUpdatableAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Updater.DataMemberUpdatableAttribute");
    }

    public static INamedTypeSymbol? DataContractAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataContractAttribute");
    }

    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass?.OriginalDefinition.Equals(attribute, SymbolEqualityComparer.Default) ?? false))
        {
            return true;
        }
        return false;
    }
}
