using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Common;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

internal abstract class MemberSymbolAnalyzer<T>(IMemberSymbolAnalyzer<T> analyzer) : IMemberSymbolAnalyzer<T>
    where T : ISymbol
{
    protected readonly IMemberSymbolAnalyzer<T> _analyzer = analyzer;

    public static bool HasDataMemberIgnore(MemberContext<IPropertySymbol> context,INamedTypeSymbol dataMemberIgnoreAttribute)
    {
        return context.Symbol.HasAttribute(dataMemberIgnoreAttribute);
    }

    public virtual SymbolInfo Analyze(MemberContext<T> symbol)
    {
        if (AppliesTo(symbol))
            return _analyzer.Analyze(symbol);
        else
            return CreateInfo();
    }
    public abstract bool AppliesTo(MemberContext<T> symbol);
    protected virtual SymbolInfo CreateInfo() => EmptyInfo.Instance;

}
