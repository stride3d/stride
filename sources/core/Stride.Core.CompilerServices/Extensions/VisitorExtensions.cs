using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices.Extensions
{
    internal static class VisitorExtensions
    {
        public static void VisitTypes(this INamespaceSymbol @namespace, Action<INamedTypeSymbol> visitor)
        {
            foreach (var type in @namespace.GetTypeMembers())
            {
                visitor(type);
            }
            foreach (var namespaceSymbol in @namespace.GetNamespaceMembers())
            {
                VisitTypes(namespaceSymbol, visitor);
            }
        }

        public static void VisitNestedTypes(this INamedTypeSymbol type, Action<INamedTypeSymbol> visitor)
        {
            foreach (var nestedType in type.GetMembers().OfType<INamedTypeSymbol>().Cast<INamedTypeSymbol>())
            {
                visitor(nestedType);
                VisitNestedTypes(nestedType, visitor);
            }
        }
    }
}
