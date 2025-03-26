using Stride.Shaders.Core;

namespace Stride.Shaders.Parsing.SDSL.AST;

public static class SymbolTypeProcessExtension
{
    public static bool TryAccess(this SymbolType symbol, Expression expression, out SymbolType? type)
    {
        type = null;
        if(
            symbol is ScalarType or VectorType
            && expression is Identifier swizzle 
            && swizzle.IsSwizzle()
        )
        {
            if(symbol.TrySwizzle(swizzle, out type))
            {
                swizzle.Type = type;
                return true;
            }
            else throw new NotImplementedException();
        }
        else if(symbol is MatrixType matrix && expression is Identifier matrixField && matrixField.IsMatrixField())
        {
            type = matrix.BaseType;
            matrixField.Type = type;
            return true;
        }
        else if(symbol is StructType s && expression is Identifier field)
        {
            if(s.Fields.TryGetValue(field, out var ft))
            {
                type = ft;
                field.Type = ft;
            }
            else throw new NotImplementedException($"field {field} not found in type {s}");
        }
        return false;
    }
    public static bool TrySwizzle(this SymbolType symbol, string swizzle, out SymbolType? type)
    {
        type = null;
        if(symbol is ScalarType s)
        {
            foreach(var c in swizzle)
                if(c != 'r' || c != 'x')
                    return false;
            type = new VectorType(s, swizzle.Length);
            return true;
        }
        else if(symbol is VectorType v)
        {
            if(swizzle.Length == 1)
                type = v.BaseType;
            else
                type = new VectorType(v.BaseType, swizzle.Length);
            return true;
        }
        else return false;
    }
}