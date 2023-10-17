using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrideSourceGenerator.NexAPI.MemberSymbolAnalysis;
internal class MemberContext<T>
    where T : ISymbol
{

    public MemberContext(T symbol, DataMemberContext context)
    {
        Symbol = symbol;
        DataMemberContext = context;
    }
    public T Symbol { get; }
    public DataMemberContext DataMemberContext { get; }
}


internal static class MemberContextExtension
{
    public static bool HasDataMemberIgnore<T>(this MemberContext<T> context, INamedTypeSymbol dataMemberIgnoreAttribute)
        where T : ISymbol
    {
        return context.Symbol.HasAttribute(dataMemberIgnoreAttribute);
    }
}
