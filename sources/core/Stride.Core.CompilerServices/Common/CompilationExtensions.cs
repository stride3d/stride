namespace Stride.Core.CompilerServices.Common;

internal static class CompilationExtensions
{
    public static IEnumerable<INamedTypeSymbol> GetAllTypesInReferences(this Compilation compilation)
    {
        foreach (var reference in compilation.References)
        {
            var referenceSymbol = compilation.GetAssemblyOrModuleSymbol(reference);
            var assemblySymbol = referenceSymbol as IAssemblySymbol ?? (referenceSymbol as IModuleSymbol)?.ContainingAssembly;
            if (assemblySymbol is not null)
            {
                foreach (var type in assemblySymbol.GetAllTypes())
                {
                    yield return type;
                }
            }
        }
    }

    public static bool IsSymbolAccessibleWithinThisAssembly(this Compilation compilation, ISymbol symbol)
    {
        return compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly);
    }
}
