using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Symbols;
internal class OriginalDefinitionFinder(IMemberSymbolAnalyzer<IPropertySymbol> analyzer, INamedTypeSymbol originalDefinition) : MemberSymbolAnalyzer<IPropertySymbol>(analyzer)
{
    private static readonly SymbolEqualityComparer Comparer = SymbolEqualityComparer.Default;

    public override bool AppliesTo(MemberContext<IPropertySymbol> context)
    {
        return context.Symbol.Type.AllInterfaces.Any(x => x.OriginalDefinition.Equals(originalDefinition, Comparer));
    }
}
internal class HasOriginalDefinitionField(IMemberSymbolAnalyzer<IFieldSymbol> analyzer, INamedTypeSymbol originalDefinition) : MemberSymbolAnalyzer<IFieldSymbol>(analyzer)
{
    private static readonly SymbolEqualityComparer Comparer = SymbolEqualityComparer.Default;

    public override bool AppliesTo(MemberContext<IFieldSymbol> context)
    {
        return context.Symbol.Type.AllInterfaces.Any(x => x.OriginalDefinition.Equals(originalDefinition, Comparer));
    }
}
