using System.Text.RegularExpressions;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Core.Analysis;

public static partial class TypeNameExtensions
{
    public static SymbolType ToSymbol(this TypeName typeName)
    {
        if(!typeName.IsArray && typeName.Generics.Count == 0 && SymbolType.TryGetNumeric(typeName.Name, out var result))
            return result!;
        else return new UndefinedType(typeName);
    }
}