using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI;
internal interface IMemberSelector
{
    IReadOnlyList<ISymbol> GetAllMembers(ITypeSymbol type);
}
