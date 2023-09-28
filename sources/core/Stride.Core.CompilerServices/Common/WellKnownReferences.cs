using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Stride.Core.CompilerServices.Common;
internal class WellKnownReferences
{
    public static INamedTypeSymbol? DataMemberAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");
    }
    public static INamedTypeSymbol? DataMemberMode(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberMode");
    }
    public static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attribute)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass?.Equals(attribute, SymbolEqualityComparer.Default) ?? false))
        {
            return true;
        }
        return false;
    }
}
