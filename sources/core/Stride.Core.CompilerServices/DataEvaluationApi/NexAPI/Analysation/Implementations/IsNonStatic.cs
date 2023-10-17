using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI.MemberSymbolAnalysis;

namespace StrideSourceGenerator.NexAPI.Implementations;
internal class IsNonStatic<T>(IMemberSymbolAnalyzer<T> analyzer) : MemberSymbolAnalyzer<T>(analyzer)
    where T : ISymbol
{
    public override bool AppliesTo(MemberContext<T> symbol)
    {
        return !symbol.Symbol.IsStatic;
    }
}