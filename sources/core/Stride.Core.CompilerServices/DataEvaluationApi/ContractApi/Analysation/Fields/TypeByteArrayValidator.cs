
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Fields;

internal class TypeByteArrayValidator(IMemberSymbolAnalyzer<IFieldSymbol> analyzer) : MemberSymbolAnalyzer<IFieldSymbol>(new TypeArrayValidator(analyzer))
{
    public override bool AppliesTo(MemberContext<IFieldSymbol> symbol)
    {
        return ((IArrayTypeSymbol)symbol.Symbol.Type).ElementType.SpecialType == SpecialType.System_Byte;
    }
}
