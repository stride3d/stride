// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Stride.Assets.Presentation.AssetEditors.VisualScriptEditor
{
    /// <summary>
    /// Various internal extensions methods taken from Roslyn (mostly from ISymbolExtensions and ITypeSymbolExtensions).
    /// </summary>
    internal static class RoslynInternalExtensions
    {
        public static IList<INamedTypeSymbol> GetAllInterfacesIncludingThis(this ITypeSymbol type)
        {
            var allInterfaces = type.AllInterfaces;
            var namedType = type as INamedTypeSymbol;
            if (namedType != null && namedType.TypeKind == TypeKind.Interface && !allInterfaces.Contains(namedType))
            {
                var result = new List<INamedTypeSymbol>(allInterfaces.Length + 1);
                result.Add(namedType);
                result.AddRange(allInterfaces);
                return result;
            }

            return allInterfaces;
        }

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static ISymbol OverriddenMember(this ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Event:
                    return ((IEventSymbol)symbol).OverriddenEvent;

                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).OverriddenMethod;

                case SymbolKind.Property:
                    return ((IPropertySymbol)symbol).OverriddenProperty;
            }

            return null;
        }
    }
}
