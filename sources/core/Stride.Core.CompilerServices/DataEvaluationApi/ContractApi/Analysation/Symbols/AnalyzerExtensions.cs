using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Symbols;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

internal static class AnalyzerExtensions
{
    internal static bool IsVisibleToEditor(this Accessibility accessibility, DataMemberContext context)
    {
        if (context.Exists)
            return accessibility == Accessibility.Public || accessibility == Accessibility.Internal;
        return accessibility == Accessibility.Public;
    }
    internal static IMemberSymbolAnalyzer<T> WhenNot<T>(this IMemberSymbolAnalyzer<T> memberAnalyzer, Func<IMemberSymbolAnalyzer<T>, IMemberSymbolAnalyzer<T>> analyzerTarget)
        where T : ISymbol
        => new NegatedAnalyzer<T>(memberAnalyzer, analyzerTarget);
    internal static IMemberSymbolAnalyzer<T> IsNotIgnored<T>(this IMemberSymbolAnalyzer<T> memberAnalyzer,IgnoreContext ignoreContext,INamedTypeSymbol dataMemberIgnoreAttribute)
        where T : ISymbol
        => new DataMemberIgnoreEvaluator<T>(memberAnalyzer,ignoreContext, dataMemberIgnoreAttribute);
    internal static IMemberSymbolAnalyzer<T> IsNonStatic<T>(this IMemberSymbolAnalyzer<T> memberAnalyzer)
    where T : ISymbol
        => new NonStaticEvaluator<T>(memberAnalyzer);
}
