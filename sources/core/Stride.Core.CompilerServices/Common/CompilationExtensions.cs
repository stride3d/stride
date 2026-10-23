namespace Stride.Core.CompilerServices.Common;

internal static class CompilationExtensions
{
    public static IEnumerable<IModuleSymbol> GetAllReferencedModules(this Compilation compilation)
    {
        foreach (var reference in compilation.References)
        {
            var referenceSymbol = compilation.GetAssemblyOrModuleSymbol(reference);
            if (referenceSymbol is IModuleSymbol moduleSymbol)
            {
                yield return moduleSymbol;
            }
            else if (referenceSymbol is IAssemblySymbol assemblySymbol)
            {
                foreach (var module in assemblySymbol.Modules)
                {
                    yield return module;
                }
            }
        }
    }

    public static bool IsSymbolAccessibleWithinThisAssembly(this Compilation compilation, ISymbol symbol)
    {
        return compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly);
    }
}
