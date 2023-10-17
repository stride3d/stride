using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI.Implementations;

internal static class FieldExtensions
{
    internal static IMemberSymbolAnalyzer<IFieldSymbol> HasOriginalDefinition(this IMemberSymbolAnalyzer<IFieldSymbol> fieldAnalyzer, INamedTypeSymbol originalDefinition)
        => new HasOriginalDefinitionField(fieldAnalyzer, originalDefinition);
    internal static IMemberSymbolAnalyzer<IFieldSymbol> IsVisibleToSerializer(this IMemberSymbolAnalyzer<IFieldSymbol> fieldAnalyzer)
        => new VisibleFieldToSerializer(fieldAnalyzer);
    internal static IMemberSymbolAnalyzer<IFieldSymbol> IsReadOnly(this IMemberSymbolAnalyzer<IFieldSymbol> fieldAnalyzer)
        => new ReadOnlyField(fieldAnalyzer);
    internal static IMemberSymbolAnalyzer<IFieldSymbol> IsArray(this IMemberSymbolAnalyzer<IFieldSymbol> fieldAnalyzer)
        => new IsArrayField(fieldAnalyzer);
    internal static IMemberSymbolAnalyzer<IFieldSymbol> IsByteArray(this IMemberSymbolAnalyzer<IFieldSymbol> fieldAnalyzer)
        => new IsByteArrayField(fieldAnalyzer);
}
