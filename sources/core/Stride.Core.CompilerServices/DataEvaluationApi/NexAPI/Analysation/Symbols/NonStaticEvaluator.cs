using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Symbols;
internal class NonStaticEvaluator<T>(IMemberSymbolAnalyzer<T> analyzer) : MemberSymbolAnalyzer<T>(analyzer)
    where T : ISymbol
{
    public override bool AppliesTo(MemberContext<T> symbol)
    {
        return !symbol.Symbol.IsStatic;
    }
}
