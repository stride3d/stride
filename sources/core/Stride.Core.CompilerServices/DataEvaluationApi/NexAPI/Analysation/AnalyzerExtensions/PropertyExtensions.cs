using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI.Implementations;

internal static class PropertyExtensions
{
    internal static IMemberSymbolAnalyzer<IPropertySymbol> HasOriginalDefinition(this IMemberSymbolAnalyzer<IPropertySymbol> propertySymbolAnalyzer, INamedTypeSymbol originalDefinition)
        => new HasOriginalDefinition(propertySymbolAnalyzer, originalDefinition);
    internal static IMemberSymbolAnalyzer<IPropertySymbol> HasVisibleGetter(this IMemberSymbolAnalyzer<IPropertySymbol> propertySymbolAnalyzer)
        => new HasVisibleGetter(propertySymbolAnalyzer);
    internal static IMemberSymbolAnalyzer<IPropertySymbol> HasVisibleSetter(this IMemberSymbolAnalyzer<IPropertySymbol> propertySymbolAnalyzer)
        => new HasVisibleSetter(propertySymbolAnalyzer);
    internal static IMemberSymbolAnalyzer<IPropertySymbol> IsArray(this IMemberSymbolAnalyzer<IPropertySymbol> propertySymbolAnalyzer)
        => new IsArrayProperty(propertySymbolAnalyzer);
    internal static IMemberSymbolAnalyzer<IPropertySymbol> IsByteArray(this IMemberSymbolAnalyzer<IPropertySymbol> propertySymbolAnalyzer)
        => new IsByteArrayProperty(propertySymbolAnalyzer);
}
