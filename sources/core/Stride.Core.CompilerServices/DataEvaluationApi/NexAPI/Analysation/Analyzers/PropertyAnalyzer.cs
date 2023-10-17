using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI.Core;
using StrideSourceGenerator.NexAPI.MemberSymbolAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrideSourceGenerator.NexAPI.Analysation.Analyzers;
internal class PropertyAnalyzer : IMemberSymbolAnalyzer<IPropertySymbol>
{
    protected readonly IContentModeInfo memberGenerator;
    internal PropertyAnalyzer(IContentModeInfo memberGenerator)
    {
        this.memberGenerator = memberGenerator;
    }

    public SymbolInfo Analyze(MemberContext<IPropertySymbol> context)
    {
        var type = context.Symbol.Type;
        var names = context.Symbol.Type.ContainingNamespace;
        string namespa = context.Symbol.Type.Name;
        if (names != null)
        {
            namespa = context.Symbol.Type.GetFullNamespace('.') + "." + context.Symbol.Type.Name;
        }
        return new SymbolInfo()
        {
            Name = context.Symbol.Name,
            TypeKind = SymbolKind.Property,
            IsAbstract = context.Symbol.Type.IsAbstract,
            IsInterface = context.Symbol.Type.TypeKind == TypeKind.Interface,
            MemberGenerator = memberGenerator,
            Type = namespa,
            DataMemberMode = memberGenerator.DataMemberMode,
            Context = context.DataMemberContext
        };
    }

    public bool AppliesTo(MemberContext<IPropertySymbol> symbol) => true;
}

