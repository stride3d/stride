using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI.MemberSymbolAnalysis;

namespace StrideSourceGenerator.NexAPI.Implementations;
internal class VisibleFieldToSerializer(IMemberSymbolAnalyzer<IFieldSymbol> analyzer) : MemberSymbolAnalyzer<IFieldSymbol>(analyzer)
{
    public override bool AppliesTo(MemberContext<IFieldSymbol> context)
    {
        return context.Symbol.DeclaredAccessibility.IsVisibleToEditor(context.DataMemberContext);
    }
}