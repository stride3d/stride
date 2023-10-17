using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.CompilerServices.Common;
using StrideSourceGenerator.NexAPI.Implementations;
using StrideSourceGenerator.NexAPI.MemberSymbolAnalysis;

namespace Stride.Core.CompilerServices.CodeFixes.NexAPI.Analysation.Implementations;
internal class IgnoreEvaluator<T>(IMemberSymbolAnalyzer<T> analyzer, IgnoreContext ignoreContext,INamedTypeSymbol dataMemberIgnoreAttribute) : MemberSymbolAnalyzer<T>(analyzer)
    where T : ISymbol
{
    public override bool AppliesTo(MemberContext<T> context)
    {
        if(ignoreContext == IgnoreContext.Yaml)
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
