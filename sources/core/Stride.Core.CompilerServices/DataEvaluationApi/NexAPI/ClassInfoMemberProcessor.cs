using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Stride.Core.CompilerServices.Common;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;
using System.Collections.Immutable;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI;
internal class ClassInfoMemberProcessor(IMemberSelector selector, Compilation compilation)
{
    public List<IMemberSymbolAnalyzer<IPropertySymbol>> PropertyAnalyzers { get; set; } = new();
    public List<IMemberSymbolAnalyzer<IFieldSymbol>> FieldAnalyzers { get; set; } = new();
    private INamedTypeSymbol DataMemberAttribute { get; } = WellKnownReferences.DataMemberAttribute(compilation);
    private INamedTypeSymbol DataMemberMode { get; } = WellKnownReferences.DataMemberMode(compilation);
    private INamedTypeSymbol DataMemberIgnoreAttribute { get; } = WellKnownReferences.DataMemberIgnoreAttribute(compilation);
    public ImmutableList<SymbolInfo> Process(ITypeSymbol type)
    {
        var symbols = selector.GetAllMembers(type);
        var result = new List<SymbolInfo>();
        foreach (var symbol in symbols)
        {
            var context = DataMemberContext.Create(symbol, DataMemberAttribute, DataMemberMode, DataMemberIgnoreAttribute);
            if (symbol == null)
                continue;
            if (symbol is IPropertySymbol property)
                ProcessAnalyzers(PropertyAnalyzers, property, result, context);
            else if (symbol is IFieldSymbol field)
            {
                ProcessAnalyzers(FieldAnalyzers, field, result, context);
            }
        }
        return ImmutableList.Create(result.ToArray());
    }
    void ProcessAnalyzers<T>(List<IMemberSymbolAnalyzer<T>> analyzers, T symbol, List<SymbolInfo> result, DataMemberContext context)
        where T : ISymbol
    {
        foreach (var analyzer in analyzers)
        {
            var memberContext = new MemberContext<T>(symbol, context);

            var temp = analyzer.Analyze(memberContext);
            if (!temp.IsEmpty)
                result.Add(temp);
        }
    }
}
