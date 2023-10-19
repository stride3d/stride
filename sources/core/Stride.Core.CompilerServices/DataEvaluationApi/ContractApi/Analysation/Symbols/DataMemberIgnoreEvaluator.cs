
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Symbols;
internal class DataMemberIgnoreEvaluator<T>(IMemberSymbolAnalyzer<T> analyzer, IgnoreContext ignoreContext, INamedTypeSymbol dataMemberIgnoreAttribute) : MemberSymbolAnalyzer<T>(analyzer)
    where T : ISymbol
{
    public override bool AppliesTo(MemberContext<T> context)
    {
        if (ignoreContext == IgnoreContext.Yaml)
        {
            if (context.HasDataMemberIgnore(dataMemberIgnoreAttribute))
            { return false; }
        }
        else
        {
            if (context.HasDataMemberIgnore(dataMemberIgnoreAttribute))
                return false;
        }
        return true;
    }
}
internal enum IgnoreContext
{
    Yaml,
    Binary
}
