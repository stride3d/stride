using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Properties;

internal class TypeArrayValidator(IMemberSymbolAnalyzer<IPropertySymbol> analyzer) : MemberSymbolAnalyzer<IPropertySymbol>(analyzer)
{
    public override bool AppliesTo(MemberContext<IPropertySymbol> context)
    {
        return context.Symbol.Type.TypeKind == TypeKind.Array;
    }
}
