using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Fields;
internal class VisibilityVerifier(IMemberSymbolAnalyzer<IFieldSymbol> analyzer) : MemberSymbolAnalyzer<IFieldSymbol>(analyzer)
{
    public override bool AppliesTo(MemberContext<IFieldSymbol> context)
    {
        return context.Symbol.DeclaredAccessibility.IsVisibleToEditor(context.DataMemberContext);
    }
}
