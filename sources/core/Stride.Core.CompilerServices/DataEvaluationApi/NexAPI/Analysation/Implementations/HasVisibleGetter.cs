using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI.Core;
using StrideSourceGenerator.NexAPI.MemberSymbolAnalysis;

internal class HasVisibleGetter(IMemberSymbolAnalyzer<IPropertySymbol> analyzer) : MemberSymbolAnalyzer<IPropertySymbol>(analyzer)
{
    public override bool AppliesTo(MemberContext<IPropertySymbol> context)
    {
        if (context.Symbol.GetMethod == null)
            return false;

        return context.Symbol.GetMethod.DeclaredAccessibility.IsVisibleToEditor(context.DataMemberContext);
    }
}
