using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Generators;

internal partial class UnifiedGenerator
{
    private sealed record TypeData(
        TypeDefinition ThisType,
        EquatableArray<TypeDefinition> Interfaces,
        EquatableArray<TypeDefinition> BaseTypes,
        EquatableArray<TypeDefinition> Attributes);
}
