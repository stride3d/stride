using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.MemberSymbolAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI.Analysation.Properties;

internal class TypeByteArrayValidator(IMemberSymbolAnalyzer<IPropertySymbol> analyzer) : MemberSymbolAnalyzer<IPropertySymbol>(new TypeArrayValidator(analyzer))
{
    public override bool AppliesTo(MemberContext<IPropertySymbol> symbol)
    {
        return ((IArrayTypeSymbol)symbol.Symbol.Type).ElementType.SpecialType == SpecialType.System_Byte;
    }
}
