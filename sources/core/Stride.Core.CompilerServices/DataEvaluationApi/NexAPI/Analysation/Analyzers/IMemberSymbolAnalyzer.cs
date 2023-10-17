
using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI.MemberSymbolAnalysis;

internal interface IMemberSymbolAnalyzer<T>
    where T : ISymbol
{
    public bool AppliesTo(MemberContext<T> symbol);
    SymbolInfo Analyze(MemberContext<T> symbol);
}