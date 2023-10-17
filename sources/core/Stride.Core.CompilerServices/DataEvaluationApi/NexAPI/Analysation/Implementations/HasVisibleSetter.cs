using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI.Core;
using StrideSourceGenerator.NexAPI.MemberSymbolAnalysis;

internal class HasVisibleSetter(IMemberSymbolAnalyzer<IPropertySymbol> analyzer) : MemberSymbolAnalyzer<IPropertySymbol>(analyzer)
{
    public override bool AppliesTo(MemberContext<IPropertySymbol> context)
    {
        if (!(context.Symbol.SetMethod != null))
            return false;

        return context.Symbol.SetMethod.DeclaredAccessibility.IsVisibleToEditor(context.DataMemberContext);
    }
}
