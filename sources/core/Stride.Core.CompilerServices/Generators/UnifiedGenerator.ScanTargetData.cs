using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Generators;

public partial class UnifiedGenerator
{
    private sealed record ScanTargetData(
        EquatableSet<TypeDefinition> Attributes,
        EquatableSet<TypeDefinition> BaseTypes,
        EquatableSet<TypeDefinition> InterfaceTypes)
    {
    }
}
