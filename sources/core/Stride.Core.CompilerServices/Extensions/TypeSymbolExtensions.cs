using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Checks if type is a closed generic type (ie. A{int}, but not A{T}).
        /// </summary>
        public static bool IsGenericInstance(this INamedTypeSymbol symbol)
            => symbol.IsGenericType && symbol.TypeArguments.All(static arg => arg.TypeKind != TypeKind.TypeParameter);

        /// <summary>
        /// Uses OriginalDefinition (reapplies generics) for full info about base types, etc.
        /// </summary>
        public static INamedTypeSymbol GetFullTypeInfo(this INamedTypeSymbol type)
        {
            var original = type.OriginalDefinition;
            if (type.IsGenericType && !type.IsUnboundGenericType && type.TypeArguments.All(static arg => arg.TypeKind != TypeKind.TypeParameter) && type.TypeArguments.Length > 0)
            {
                return original.ConstructedFrom.Construct(type.TypeArguments, type.TypeArgumentNullableAnnotations);
            }
            else if (type.IsGenericType && type.TypeArguments.All(static arg => arg.TypeKind == TypeKind.TypeParameter) && type.TypeArguments.Length > 0)
            {
                return original.ConstructUnboundGenericType();
            }
            // TODO: figure out how to freshen up a nested class of a generic class
            return original;
        }
    }
}
