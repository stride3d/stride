namespace Stride.Core.CompilerServices.Common;

internal static class CompilationExtensions
{
    public static bool IsSymbolAccessibleWithinThisAssembly(this Compilation compilation, ISymbol symbol)
    {
        return compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly);
    }
}
