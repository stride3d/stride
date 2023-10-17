using Microsoft.CodeAnalysis;
using StrideSourceGenerator.NexAPI;
using StrideSourceGenerator.NexAPI.Core;
using StrideSourceGenerator.NexIncremental;

namespace StrideSourceGenerator.Core;
internal class MemberSelector(INamedTypeSymbol decidingAttribute) : IMemberSelector
{
    public IReadOnlyList<ISymbol> GetAllMembers(ITypeSymbol type)
    {
        List<ITypeSymbol> types = new List<ITypeSymbol>();
        var trial = type.FindAttributeInInheritanceTree(decidingAttribute);
        
        FindTypesRecursive(decidingAttribute, types, type);
 
        return GetMembers(types);
    }
    private ITypeSymbol FindTypesRecursiveUnchecked(ITypeSymbol breakPoint, List<ITypeSymbol> members, ITypeSymbol baseType)
    {
        while (baseType is not null)
        {
            members.Add(baseType);
            if (baseType.Equals(breakPoint, SymbolEqualityComparer.Default))
                return baseType;
        }
        return null;
    }
    private static void FindTypesRecursive(INamedTypeSymbol decidingAttribute, List<ITypeSymbol> members, ITypeSymbol baseType)
    {
        while (baseType != null)
        {
            if (baseType.HasAttribute(decidingAttribute))
                members.Add(baseType);
            baseType = baseType.BaseType;
        }
    }

    private IReadOnlyList<ISymbol> GetMembers(IReadOnlyList<ITypeSymbol> typeSymbols)
    {
        List<ISymbol> result = new List<ISymbol>();
        HashSet<string> seenMembers = new HashSet<string>();


        foreach (ITypeSymbol type in typeSymbols)
        {
            foreach (var member in type.GetMembers())
            {
                if(member is IPropertySymbol property && !property.IsIndexer && !seenMembers.Contains(member.Name))
                {
                    seenMembers.Add(member.Name);
                    result.Add(member);
                }
                if ((member is IFieldSymbol) && !seenMembers.Contains(member.Name))
                {
                    seenMembers.Add(member.Name);
                    result.Add(member);
                }
            }
        }

        return result;
    }
}
