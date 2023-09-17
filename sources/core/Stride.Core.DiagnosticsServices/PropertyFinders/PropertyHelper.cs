using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StrideDiagnostics.PropertyFinders;

internal static class PropertyHelper
{
    public static bool IsArray(IPropertySymbol propertyInfo)
    {
        ITypeSymbol propertyType = propertyInfo.Type;

        if (propertyType.TypeKind == TypeKind.Array)
        {
            return true;
        }
        return false;
    }
    public static bool ImplementsICollectionT(ITypeSymbol type)
    {
        if (type.AllInterfaces.Any(i =>
            (i.OriginalDefinition is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_ICollection_T)))
        {
            return true;
        }

        return false;
    }
    public static bool IsDictionary(IPropertySymbol type, ClassInfo info)
    {

        INamedTypeSymbol dictionaryInterface = info.ExecutionContext.Compilation.GetTypeByMetadataName(typeof(IDictionary<,>).FullName);
        SymbolEqualityComparer comparer = SymbolEqualityComparer.Default;
        if (type.Type.AllInterfaces.Any(x => x.OriginalDefinition.Equals(dictionaryInterface, comparer)))
        {
            return true;
        }

        return false;
    }
}