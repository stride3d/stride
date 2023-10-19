using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.DataEvaluationApi.DataApi;

public static class Extensions
{
    public static bool HasInheritedDataContractAttributeInInheritanceHierarchy(this ITypeSymbol typeDeclaration, INamedTypeSymbol attribute)
    {
        if (typeDeclaration == null) return false;
        var baseType = typeDeclaration;

        while (baseType != null)
        {
            if (baseType.HasAttribute(attribute))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }
}
