
using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI.MemberSymbolAnalysis;

internal class WhenNot<T>(
        IMemberSymbolAnalyzer<T> analyzer
        , Func<IMemberSymbolAnalyzer<T>, IMemberSymbolAnalyzer<T>> analyzerTarget
    ) : MemberSymbolAnalyzer<T>(analyzer)
    where T : ISymbol
{
    public override bool AppliesTo(MemberContext<T> symbol)
    {
        return !analyzerTarget.Invoke(base._analyzer).AppliesTo(symbol);
    }
}