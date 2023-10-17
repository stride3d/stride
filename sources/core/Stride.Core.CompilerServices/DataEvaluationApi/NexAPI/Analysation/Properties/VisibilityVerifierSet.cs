using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

internal class VisibilityVerifierSet(IMemberSymbolAnalyzer<IPropertySymbol> analyzer) : MemberSymbolAnalyzer<IPropertySymbol>(analyzer)
{
    public override bool AppliesTo(MemberContext<IPropertySymbol> context)
    {
        if (!(context.Symbol.SetMethod != null))
            return false;

        return context.Symbol.SetMethod.DeclaredAccessibility.IsVisibleToEditor(context.DataMemberContext);
    }
}
