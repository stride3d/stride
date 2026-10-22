using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.CompilerServices.Generators;

internal partial class UnifiedGenerator
{
    private sealed record AssemblyDefinition(string Name) : BaseDefinition
    {
        public override string FullName => Name;

        [return: NotNullIfNotNull(nameof(symbol))]
        public static AssemblyDefinition? FromSymbol(IAssemblySymbol? symbol)
        {
            if (symbol is null)
                return null;
            return new AssemblyDefinition(symbol.Name);
        }
    }
}
