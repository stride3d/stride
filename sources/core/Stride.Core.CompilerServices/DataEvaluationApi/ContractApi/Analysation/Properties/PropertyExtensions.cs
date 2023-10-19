

using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Properties;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Symbols;

internal static class PropertyExtensions
{
    internal static IMemberSymbolAnalyzer<IPropertySymbol> HasOriginalDefinition(this IMemberSymbolAnalyzer<IPropertySymbol> propertySymbolAnalyzer, INamedTypeSymbol originalDefinition)
        => new OriginalDefinitionFinder(propertySymbolAnalyzer, originalDefinition);
    internal static IMemberSymbolAnalyzer<IPropertySymbol> HasVisibleGetter(this IMemberSymbolAnalyzer<IPropertySymbol> propertySymbolAnalyzer)
        => new VisibilityVerifierGet(propertySymbolAnalyzer);
    internal static IMemberSymbolAnalyzer<IPropertySymbol> HasVisibleSetter(this IMemberSymbolAnalyzer<IPropertySymbol> propertySymbolAnalyzer)
        => new VisibilityVerifierSet(propertySymbolAnalyzer);
    internal static IMemberSymbolAnalyzer<IPropertySymbol> IsArray(this IMemberSymbolAnalyzer<IPropertySymbol> propertySymbolAnalyzer)
        => new TypeArrayValidator(propertySymbolAnalyzer);
    internal static IMemberSymbolAnalyzer<IPropertySymbol> IsByteArray(this IMemberSymbolAnalyzer<IPropertySymbol> propertySymbolAnalyzer)
        => new TypeByteArrayValidator(propertySymbolAnalyzer);
}
