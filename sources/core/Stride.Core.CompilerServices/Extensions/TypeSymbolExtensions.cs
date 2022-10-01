using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices.Extensions
{
    public static class TypeSymbolExtensions
    {
        public static bool Is(this ITypeSymbol symbol, ITypeSymbol other) => symbol.Equals(other, SymbolEqualityComparer.Default);

        public static bool IsGeneric(this INamedTypeSymbol symbol, INamedTypeSymbol other)
        {
            if (!symbol.IsGenericType || !other.IsGenericType)
            {
                return false;
            }

            return symbol.OriginalDefinition.ConstructUnboundGenericType().Is(other.OriginalDefinition.ConstructUnboundGenericType());
        }
    }
}
