using Microsoft.CodeAnalysis;
using Stride.Core.StrideDiagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Core.StrideDiagnostics.PropertyFinders;

internal static class PropertyHelper
{
    public static bool IsArray(IPropertySymbol propertyInfo)
    {
        var propertyType = propertyInfo.Type;

        if (propertyType.TypeKind == TypeKind.Array)
        {
            return true;
        }
        return false;
    }
    public static bool ImplementsICollectionT(ITypeSymbol type)
    {
        if (type.AllInterfaces.Any(i =>
            i.OriginalDefinition is INamedTypeSymbol namedType && namedType.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_ICollection_T))
        {
            return true;
        }

        return false;
    }
    public static bool IsDictionary(IPropertySymbol type, ClassInfo info)
    {

        var dictionaryInterface = info.ExecutionContext.Compilation.GetTypeByMetadataName(typeof(IDictionary<,>).FullName);
        var comparer = SymbolEqualityComparer.Default;
        if (type.Type.AllInterfaces.Any(x => x.OriginalDefinition.Equals(dictionaryInterface, comparer)))
        {
            return true;
        }

        return false;
    }
}