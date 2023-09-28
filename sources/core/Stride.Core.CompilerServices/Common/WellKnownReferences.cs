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

    public static INamedTypeSymbol? DataMemberIgnoreAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberIgnoreAttribute");
    }
    public static INamedTypeSymbol? DataMemberMode(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberMode");
    }    
    public static INamedTypeSymbol? DataMemberUpdatableAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Updater.DataMemberUpdatableAttribute");
    }
    public static INamedTypeSymbol? DataContractAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataContractAttribute");
    }
    public static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attribute)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass?.Equals(attribute, SymbolEqualityComparer.Default) ?? false))
        {
            return true;
        }
        return false;
    }
    public static bool HasDataContractAttribute(INamedTypeSymbol baseType)
    {
        return baseType.GetAttributes().Any(attr =>
                    attr.AttributeClass?.Name == "DataContractAttribute"
                     && attr.AttributeClass.ContainingNamespace.ContainingModule.Name == "Stride.Core.dll");
    }
}
