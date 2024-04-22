using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Symbols;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Fields;
internal static class FieldExtensions
{
    internal static IMemberSymbolAnalyzer<IFieldSymbol> HasOriginalDefinition(this IMemberSymbolAnalyzer<IFieldSymbol> fieldAnalyzer, INamedTypeSymbol originalDefinition)
        => new HasOriginalDefinitionField(fieldAnalyzer, originalDefinition);
    internal static IMemberSymbolAnalyzer<IFieldSymbol> IsVisibleToSerializer(this IMemberSymbolAnalyzer<IFieldSymbol> fieldAnalyzer)
        => new VisibilityVerifier(fieldAnalyzer);
    internal static IMemberSymbolAnalyzer<IFieldSymbol> IsReadOnly(this IMemberSymbolAnalyzer<IFieldSymbol> fieldAnalyzer)
        => new ReadOnlyField(fieldAnalyzer);
    internal static IMemberSymbolAnalyzer<IFieldSymbol> IsArray(this IMemberSymbolAnalyzer<IFieldSymbol> fieldAnalyzer)
        => new TypeArrayValidator(fieldAnalyzer);
    internal static IMemberSymbolAnalyzer<IFieldSymbol> IsByteArray(this IMemberSymbolAnalyzer<IFieldSymbol> fieldAnalyzer)
        => new TypeByteArrayValidator(fieldAnalyzer);
}
