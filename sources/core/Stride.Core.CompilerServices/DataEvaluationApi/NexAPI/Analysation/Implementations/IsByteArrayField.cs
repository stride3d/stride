using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI.MemberSymbolAnalysis;

namespace StrideSourceGenerator.NexAPI.Implementations;

internal class IsByteArrayField(IMemberSymbolAnalyzer<IFieldSymbol> analyzer) : MemberSymbolAnalyzer<IFieldSymbol>(new IsArrayField(analyzer))
{
    public override bool AppliesTo(MemberContext<IFieldSymbol> symbol)
    {
        return ((IArrayTypeSymbol)symbol.Symbol.Type).ElementType.SpecialType == SpecialType.System_Byte;
    }
}