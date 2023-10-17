using Microsoft.CodeAnalysis;

namespace StrideSourceGenerator.NexAPI;
internal interface IMemberSelector
{
    IReadOnlyList<ISymbol> GetAllMembers(ITypeSymbol type);
}
