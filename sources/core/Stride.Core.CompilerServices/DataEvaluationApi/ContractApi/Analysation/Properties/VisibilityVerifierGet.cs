using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

internal class VisibilityVerifierGet(IMemberSymbolAnalyzer<IPropertySymbol> analyzer) : MemberSymbolAnalyzer<IPropertySymbol>(analyzer)
{
    public override bool AppliesTo(MemberContext<IPropertySymbol> context)
    {
        if (context.Symbol.GetMethod == null)
            return false;

        return context.Symbol.GetMethod.DeclaredAccessibility.IsVisibleToEditor(context.DataMemberContext);
    }
}
