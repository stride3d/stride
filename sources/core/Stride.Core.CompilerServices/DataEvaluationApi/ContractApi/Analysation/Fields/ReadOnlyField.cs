using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Fields;
internal class ReadOnlyField(IMemberSymbolAnalyzer<IFieldSymbol> analyzer) : MemberSymbolAnalyzer<IFieldSymbol>(analyzer)
{
    public override bool AppliesTo(MemberContext<IFieldSymbol> context)
    {
        var symbol = context.Symbol;
        if (!symbol.IsReadOnly)
            return false;
        if (symbol.Type.TypeKind == TypeKind.Struct || symbol.Type.SpecialType == SpecialType.System_String)
            return false;
        return true;
    }
}
