using System.Text;

namespace Stride.Core.CompilerServices.Common;

internal static class TypeNameBuilder
{
    /// <summary>
    /// Get the reflection name of a type, including its assembly name if available.
    /// </summary>
    /// <param name="type">The type symbol to get the reflection name for.</param>
    /// <returns>The reflection name of the type, or null if the type is not supported.</returns>
    public static string? GetReflectionName(this ITypeSymbol type)
    {
        return GetName(type, unsafeAccessorType: false);
    }

    /// <summary>
    /// Get the UnsafeAccessorType name of a type.
    /// </summary>
    /// <param name="type">The type symbol to get the UnsafeAccessorType name for.</param>
    /// <returns>The UnsafeAccessorType name of the type, or null if the type is not supported.</returns>
    public static string? GetUnsafeAccessorTypeName(this ITypeSymbol type)
    {
        return GetName(type, unsafeAccessorType: true);
    }

    private static string? GetName(ITypeSymbol type, bool unsafeAccessorType)
    {
        var typeWithoutAssembly = Build(type, unsafeAccessorType);
        if (typeWithoutAssembly is null)
            return null;
        var assembly = RootAssembly(type);
        return assembly is null
            ? typeWithoutAssembly
            : $"{typeWithoutAssembly}, {assembly.Identity.Name}";
    }

    private static string? Build(ITypeSymbol type, bool unsafeAccessorType)
    {
        switch (type)
        {
            case IArrayTypeSymbol arr:
                var elementTypeString = Build(arr.ElementType, unsafeAccessorType);
                if (elementTypeString is null)
                    return null;
                var rank = arr.Rank == 1 ? "[]" : $"[{new string(',', arr.Rank - 1)}]";
                return elementTypeString + rank;

            case IPointerTypeSymbol ptr:
                var pointedAtTypeString = Build(ptr.PointedAtType, unsafeAccessorType);
                if (pointedAtTypeString is null)
                    return null;
                return pointedAtTypeString + "*";

            case IErrorTypeSymbol:
                return null;

            case INamedTypeSymbol named:
                if (named.IsAnonymousType)
                    return null;
                var sb = new StringBuilder();
                AppendDefinitionName(sb, named);
                if (!named.IsDefinition || unsafeAccessorType)
                {
                    // Unsafe accessor types have type parameters specified for both open and closed generic types.
                    // Reflection types only have type parameters specified for closed generic types.
                    if (!AppendTypeArguments(sb, named, unsafeAccessorType))
                        return null;
                }
                return sb.ToString();

            case IDynamicTypeSymbol:
                return "System.Object";

            case ITypeParameterSymbol typeParameter:
                if (!unsafeAccessorType)
                    return null;
                return typeParameter.TypeParameterKind switch
                {
                    TypeParameterKind.Type => $"!{typeParameter.Ordinal + typeParameter.ContainingType.ContainingType.GetTotalArity()}",
                    TypeParameterKind.Method => $"!!{typeParameter.Ordinal}",
                    _ => null
                };

            default:
                return null;
        }
    }

    private static void AppendNamespace(StringBuilder sb, INamespaceSymbol ns)
    {
        if (!ns.IsGlobalNamespace)
        {
            AppendNamespace(sb, ns.ContainingNamespace);
            sb.Append(ns.Name).Append('.');
        }
    }

    private static void AppendDefinitionName(StringBuilder sb, INamedTypeSymbol type)
    {
        if (type.ContainingType is { } outer)
        {
            AppendDefinitionName(sb, outer);
            sb.Append('+');
        }
        else
        {
            AppendNamespace(sb, type.ContainingNamespace);
        }
        sb.Append(type.MetadataName); // includes `N arity
    }

    private static bool AppendTypeArguments(StringBuilder sb, INamedTypeSymbol type, bool unsafeAccessorType)
    {
        // CLR flattens type args: Outer<int>.Inner<string> => Outer`1+Inner`1[[int],[string]]
        var args = CollectTypeArguments(type);
        if (args.Count > 0)
        {
            sb.Append('[');
            for (var i = 0; i < args.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                var argumentString = GetName(args[i], unsafeAccessorType);
                if (argumentString is null)
                    return false;
                sb.Append('[').Append(argumentString).Append(']');
            }
            sb.Append(']');
        }
        return true;
    }

    private static IReadOnlyList<ITypeSymbol> CollectTypeArguments(INamedTypeSymbol type)
    {
        if (type.ContainingType is { } outer)
            return [.. CollectTypeArguments(outer), .. type.TypeArguments];
        else
            return type.TypeArguments;
    }

    private static IAssemblySymbol? RootAssembly(ITypeSymbol t) => t switch
    {
        IArrayTypeSymbol a => RootAssembly(a.ElementType),
        IPointerTypeSymbol p => RootAssembly(p.PointedAtType),
        ITypeParameterSymbol => null,
        IDynamicTypeSymbol => null,
        _ => t.ContainingAssembly
    };
}
