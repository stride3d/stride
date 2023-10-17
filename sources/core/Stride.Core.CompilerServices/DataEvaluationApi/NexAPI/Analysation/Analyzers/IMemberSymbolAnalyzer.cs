
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

internal interface IMemberSymbolAnalyzer<T>
    where T : ISymbol
{
    public bool AppliesTo(MemberContext<T> symbol);
    SymbolInfo Analyze(MemberContext<T> symbol);
}