using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices.Extensions
{
    public static class SybmbolFormatExtensions
    {
        public static readonly SymbolDisplayFormat SimpleClassNameWithNestedInfo = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

        /// <summary>
        /// Returns display string for <paramref name="typeSymbol"/> in <see cref="SimpleClassNameWithNestedInfo"/> format.
        /// </summary>
        public static string ToStringSimpleClass(this ITypeSymbol typeSymbol) => typeSymbol.ToDisplayString(SimpleClassNameWithNestedInfo);
    }
}
