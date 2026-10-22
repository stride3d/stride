using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stride.Core.CompilerServices.Common;

namespace Stride.Core.CompilerServices.Generators;

[Generator]
internal class UnifiedGenerator : IIncrementalGenerator
{
    private const int ModuleInitializerOrderForAssemblyScanResults = -1000;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var referenceDataProvider = context.CompilationProvider.Select((compilation, _) =>
        {
            List<INamedTypeSymbol> assemblyScanAttributes = [];
            List<INamedTypeSymbol> assemblyScanBaseTypes = [];
            List<INamedTypeSymbol> assemblyScanInterfaceTypes = [];

            foreach (var type in compilation.GetAllTypesInReferences())
            {
                if (!HasAssemblyScanAttribute(type))
                {
                }
                else if (!compilation.IsSymbolAccessibleWithinThisAssembly(type))
                {
                }
                else if (type.TypeKind == TypeKind.Interface)
                {
                    assemblyScanInterfaceTypes.Add(type);
                }
                else if (InheritsFromSystemAttribute(type))
                {
                    assemblyScanAttributes.Add(type);
                }
                else if (!type.IsSealed)
                {
                    assemblyScanBaseTypes.Add(type);
                }
            }
            return new ScanTargetData(
                new EquatableSet<TypeDefinition>(assemblyScanAttributes.Select(TypeDefinition.FromSymbol)),
                new EquatableSet<TypeDefinition>(assemblyScanBaseTypes.Select(TypeDefinition.FromSymbol)),
                new EquatableSet<TypeDefinition>(assemblyScanInterfaceTypes.Select(TypeDefinition.FromSymbol))
            );
        });

        var sourceInterfaceProvider = context.SyntaxProvider.ForAttributeWithMetadataName(WellKnownReferences.AssemblyScanAttributeName, (node, _) => node is InterfaceDeclarationSyntax, (context, _) =>
        {
            var type = (INamedTypeSymbol)context.TargetSymbol;

            if (!context.SemanticModel.Compilation.IsSymbolAccessibleWithinThisAssembly(type))
                return null;

            return TypeDefinition.FromSymbol(type);
        }).WhereNotNull().Collect();

        var sourceClassProvider = context.SyntaxProvider.ForAttributeWithMetadataName(WellKnownReferences.AssemblyScanAttributeName, (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax, (context, _) =>
        {
            if (context.TargetSymbol is not INamedTypeSymbol type || type.IsSealed)
                return null;

            if (InheritsFromSystemAttribute(type))
                return null;

            if (!context.SemanticModel.Compilation.IsSymbolAccessibleWithinThisAssembly(type))
                return null;

            return TypeDefinition.FromSymbol(type);
        }).WhereNotNull().Collect();

        var sourceAttributeProvider = context.SyntaxProvider.ForAttributeWithMetadataName(WellKnownReferences.AssemblyScanAttributeName, (node, _) => node is ClassDeclarationSyntax, (context, _) =>
        {
            if (context.TargetSymbol is not INamedTypeSymbol type)
                return null;

            if (!InheritsFromSystemAttribute(type))
                return null;

            if (!context.SemanticModel.Compilation.IsSymbolAccessibleWithinThisAssembly(type))
                return null;

            return TypeDefinition.FromSymbol(type);
        }).WhereNotNull().Collect();

        var scanTargetDataProvider = sourceInterfaceProvider.Combine(sourceClassProvider).Combine(sourceAttributeProvider.Combine(referenceDataProvider)).Select((set, _) =>
        {
            var ((interfaceTypes, baseTypes), (attributeTypes, referenceData)) = set;
            return new ScanTargetData(
                new EquatableSet<TypeDefinition>(attributeTypes.Concat(referenceData.Attributes)),
                new EquatableSet<TypeDefinition>(baseTypes.Concat(referenceData.BaseTypes)),
                new EquatableSet<TypeDefinition>(interfaceTypes.Concat(referenceData.InterfaceTypes))
            );
        });

        var typeDataProvider = context.SyntaxProvider.CreateSyntaxProvider((node, _) =>
        {
            return node is BaseTypeDeclarationSyntax;
        },
        (context, cancellationToken) =>
        {
            if (context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) is not INamedTypeSymbol symbol)
                return null;

            if (!context.Node.IsFirstSyntaxReference(symbol))
                return null;

            // Exclude private nested types in this assembly, as they cannot be accessed from our generated code.
            if (!context.SemanticModel.Compilation.IsSymbolAccessibleWithinThisAssembly(symbol))
                return null;

            var thisType = TypeDefinition.FromSymbol(symbol);
            var interfaces = symbol.AllInterfaces.Select(i => TypeDefinition.FromSymbol(i.OriginalDefinition)).ToArray();
            var baseTypes = symbol.GetAllBaseTypesAscending().Select(t => TypeDefinition.FromSymbol(t.OriginalDefinition)).ToArray();
            var attributes = GetAllAttributeTypes(symbol).Select(TypeDefinition.FromSymbol).Distinct().ToArray();

            return interfaces.Length != 0 || baseTypes.Length != 0 || attributes.Length != 0
                ? new TypeData(thisType, interfaces, baseTypes, attributes)
                : null;
        }).WhereNotNull();

        var scanResultsProvider = typeDataProvider.Combine(scanTargetDataProvider).SelectMany((pair, _) =>
        {
            var (typeData, scanTargetData) = pair;
            List<(string ScanType, string ThisType)>? results = null;
            foreach (var @interface in typeData.Interfaces)
            {
                if (scanTargetData.InterfaceTypes.Contains(@interface))
                {
                    results ??= [];
                    results.Add((@interface.UnboundFullName, typeData.ThisType.UnboundFullName));
                }
            }
            if (scanTargetData.BaseTypes.Contains(typeData.ThisType))
            {
                results ??= [];
                results.Add((typeData.ThisType.UnboundFullName, typeData.ThisType.UnboundFullName));
            }
            foreach (var baseType in typeData.BaseTypes)
            {
                if (scanTargetData.BaseTypes.Contains(baseType))
                {
                    results ??= [];
                    results.Add((baseType.UnboundFullName, typeData.ThisType.UnboundFullName));
                }
            }
            foreach (var attribute in typeData.Attributes)
            {
                if (scanTargetData.Attributes.Contains(attribute))
                {
                    results ??= [];
                    results.Add((attribute.UnboundFullName, typeData.ThisType.UnboundFullName));
                }
            }
            return (IEnumerable<(string ScanType, string ThisType)>?)results ?? [];
        }).Collect();

        var moduleInitializerProvider = context.SyntaxProvider.ForAttributeWithMetadataName(WellKnownReferences.ModuleInitializerAttributeName,
            (node, transform) =>
            {
                return node is MethodDeclarationSyntax { ReturnType: PredefinedTypeSyntax ret } mds
                    && ret.Keyword.IsKind(SyntaxKind.VoidKeyword)
                    && mds.ParameterList.Parameters is []
                    && mds.Arity == 0
                    && mds.Modifiers.Any(t => t.IsKind(SyntaxKind.StaticKeyword));
            },
            (ctx, transform) =>
            {
                if (ctx.TargetSymbol is not IMethodSymbol method)
                    return default;

                int order = 0;
                var moduleInitializerType = WellKnownReferences.ModuleInitializerAttribute(ctx.SemanticModel.Compilation)!;
                if (method.TryGetAttribute(moduleInitializerType, out var attributeData))
                {
                    if (attributeData is { AttributeConstructor.Parameters: [{ Name: "order" }, ..], ConstructorArguments: [{ Value: int orderNumber }, ..] })
                    {
                        order = orderNumber;
                    }
                }

                var className = $"{method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}";

                return new OrderedModuleInitializerMethod(order, className);
            }
        ).Where(m => m != default).Collect();

        context.RegisterImplementationSourceOutput(scanResultsProvider.Combine(moduleInitializerProvider), (context, pair) =>
        {
            var (scanResults, moduleInitializers) = pair;
            var moduleInitializersClass = GenerateModuleInitializersClass(moduleInitializers, !scanResults.IsEmpty);
            var scanResultsClass = GenerateScanResultsClass(scanResults);
            if (moduleInitializersClass is not null || scanResultsClass is not null)
            {
                context.AddSource("ModuleInitializersAndAssemblyScanResults.g.cs", $"""
                    // <auto-generated/>
                    {moduleInitializersClass}
                    {scanResultsClass}
                    """);
            }
        });
    }

    private static string? GenerateModuleInitializersClass(ImmutableArray<OrderedModuleInitializerMethod> moduleInitializers, bool hasAssemblyScanResults)
    {
        if (moduleInitializers.Length == 0 && !hasAssemblyScanResults)
            return null;

        var code = new StringBuilder();
        var allModuleInitializers = hasAssemblyScanResults
            ? moduleInitializers.Append(new OrderedModuleInitializerMethod(ModuleInitializerOrderForAssemblyScanResults, "global::Stride.Core.Serialization.AssemblyScan.AssemblyScanResults.Initialize"))
            : moduleInitializers;
        foreach (var method in allModuleInitializers.OrderBy(x => x.Order))
        {
            code.AppendLine($"            {method.InvocationString}();");
        }

        return $$"""
            namespace Stride.Core.CompilerServices.AutoGenerated.ModuleInitializer
            {
                file static class ModuleInitializer
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    public static void Initialize()
                    {
            {{code}}        }
                }
            }
            """;
    }

    private static string? GenerateScanResultsClass(ImmutableArray<(string ScanType, string ThisType)> scanResults)
    {
        if (scanResults.IsEmpty)
            return null;

        string indent0 = new string(' ', 8);
        string indent1 = new string(' ', 24);
        string indent2 = new string(' ', 28);

        var attributes = new StringBuilder();
        foreach (var thisType in scanResults.Select(r => r.ThisType).Distinct())
        {
            attributes.Append(indent0);
            attributes.Append("[global::System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors, typeof(global::");
            attributes.Append(thisType);
            attributes.AppendLine("))]");
        }

        var sb = new StringBuilder();
        foreach (var group in scanResults.GroupBy(r => r.ScanType, r => r.ThisType))
        {
            sb.Append(indent1);
            sb.AppendLine("{");

            sb.Append(indent2);
            sb.Append("typeof(global::");
            sb.Append(group.Key);
            sb.AppendLine("),");

            sb.Append(indent2);
            sb.Append("new global::System.Collections.Generic.List<global::System.Type> { ");
            foreach (var type in group.OrderBy(t => t, StringComparer.Ordinal))
            {
                sb.Append("typeof(global::");
                sb.Append(type);
                sb.Append("), ");
            }
            sb.AppendLine("}");

            sb.Append(indent1);
            sb.AppendLine("},");
        }

        return $$"""
            namespace Stride.Core.Serialization.AssemblyScan
            {
                file static partial class AssemblyScanResults
                {
                    [global::System.Runtime.CompilerServices.ModuleInitializer]
            {{attributes}}
                    public static void Initialize()
                    {
                        global::Stride.Core.Reflection.AssemblyRegistry.RegisterScanTypes(
                            typeof(global::Stride.Core.Serialization.AssemblyScan.AssemblyScanResults).Assembly,
                            new global::Stride.Core.Reflection.AssemblyRegistry.ScanTypes(
                                new global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Collections.Generic.List<global::System.Type>>
                                {
            {{sb}}
                                }
                            )
                        );
                    }
                }
            }
            """;
    }

    private static bool InheritsFromSystemAttribute(INamedTypeSymbol type)
    {
        while (type.BaseType is not null)
        {
            if (type.BaseType is { Name: "Attribute", ContainingNamespace: { Name: "System", ContainingNamespace: null or { IsGlobalNamespace: true } } })
                return true;
            type = type.BaseType;
        }
        return false;
    }

    private static IEnumerable<INamedTypeSymbol> GetAllAttributeTypes(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass is not null)
                yield return attribute.AttributeClass;
        }

        // Get attributes from base types if they are inherited
        // Note: We only check base types, not interfaces, because attributes are not inherited from interfaces
        var baseType = type.BaseType?.OriginalDefinition;
        while (baseType is not null)
        {
            foreach (var attribute in baseType.GetAttributes())
            {
                if (attribute.AttributeClass is not null && IsAttributeUsageInherited(attribute.AttributeClass))
                    yield return attribute.AttributeClass;
            }
            baseType = baseType.BaseType?.OriginalDefinition;
        }
    }

    /// <summary>
    /// Checks if the given attribute type has the AttributeUsageAttribute with Inherited set to true.
    /// </summary>
    /// <param name="type">The attribute type to check.</param>
    /// <returns>True if the attribute type has the AttributeUsageAttribute with Inherited set to true; otherwise, false.</returns>
    private static bool IsAttributeUsageInherited(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            bool isAttributeUsageAttribute = attribute.AttributeClass is
            {
                Name: nameof(AttributeUsageAttribute),
                ContainingNamespace:
                {
                    Name: nameof(System),
                    ContainingNamespace: null or { IsGlobalNamespace: true }
                }
            };
            if (isAttributeUsageAttribute)
            {
                return attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == nameof(AttributeUsageAttribute.Inherited)).Value.Value is not false;
            }
        }

        // If the type does not have an AttributeUsageAttribute, check its base type
        // This is because the runtime will use the AttributeUsageAttribute from the base type if
        // the derived type does not have one.
        var baseType = type.BaseType?.OriginalDefinition;
        return baseType is not null && IsAttributeUsageInherited(baseType);
    }

    private static bool HasAssemblyScanAttribute(ISymbol symbol)
    {
        // Stride.Core.Reflection.AssemblyScanAttribute
        foreach (var attribute in symbol.GetAttributes())
        {
            bool isAssemblyScanAttribute = attribute.AttributeClass is
            {
                Name: "AssemblyScanAttribute",
                ContainingNamespace:
                {
                    Name: "Reflection",
                    ContainingNamespace:
                    {
                        Name: "Core",
                        ContainingNamespace:
                        {
                            Name: "Stride",
                            ContainingNamespace: null or { IsGlobalNamespace: true }
                        }
                    }
                }
            };
            if (isAssemblyScanAttribute)
                return true;
        }
        return false;
    }

    private sealed record TypeData(
        TypeDefinition ThisType,
        EquatableArray<TypeDefinition> Interfaces,
        EquatableArray<TypeDefinition> BaseTypes,
        EquatableArray<TypeDefinition> Attributes);

    private sealed record ScanTargetData(
        EquatableSet<TypeDefinition> Attributes,
        EquatableSet<TypeDefinition> BaseTypes,
        EquatableSet<TypeDefinition> InterfaceTypes)
    {
    }

    private abstract record BaseDefinition
    {
        public abstract string FullName { get; }
    }
    private sealed record AssemblyDefinition(string Name) : BaseDefinition
    {
        public override string FullName => Name;

        [return: NotNullIfNotNull(nameof(symbol))]
        public static AssemblyDefinition? FromSymbol(IAssemblySymbol? symbol)
        {
            if (symbol is null)
                return null;
            return new AssemblyDefinition(symbol.Name);
        }
    }
    private sealed record NamespaceDefinition(string Name, BaseDefinition? Parent) : BaseDefinition
    {
        public override string FullName
        {
            get
            {
                return field ??= Parent is NamespaceDefinition ? $"{Parent.FullName}.{Name}" : Name;
            }
        }

        public static NamespaceDefinition FromSymbol(INamespaceSymbol symbol)
        {
            BaseDefinition? parent;
            if (symbol.ContainingNamespace is not null && !symbol.ContainingNamespace.IsGlobalNamespace)
            {
                parent = FromSymbol(symbol.ContainingNamespace);
            }
            else
            {
                parent = AssemblyDefinition.FromSymbol(symbol.ContainingAssembly);
            }
            return new NamespaceDefinition(symbol.Name, parent);
        }
    }
    private sealed record TypeDefinition(string Name, BaseDefinition? Parent, EquatableArray<string> GenericParameters) : BaseDefinition
    {
        public TypeDefinition(string Name, BaseDefinition? Parent) : this(Name, Parent, EquatableArray<string>.Empty)
        {
        }

        public override string FullName
        {
            get
            {
                if (field is null)
                {
                    var genericParameters = GenericParameters.Count > 0 ? $"<{string.Join(", ", GenericParameters)}>" : "";
                    field = Parent is NamespaceDefinition or TypeDefinition ? $"{Parent.FullName}.{Name}{genericParameters}" : $"{Name}{genericParameters}";
                }
                return field;
            }
        }

        public string UnboundFullName
        {
            get
            {
                if (field is null)
                {
                    var genericParameters = GenericParameters.Count > 0 ? $"<{new string(',', GenericParameters.Count - 1)}>" : "";
                    if (Parent is NamespaceDefinition)
                    {
                        field = $"{Parent.FullName}.{Name}{genericParameters}";
                    }
                    else if (Parent is TypeDefinition declaringType)
                    {
                        field = $"{declaringType.UnboundFullName}.{Name}{genericParameters}";
                    }
                    else
                    {
                        field = $"{Name}{genericParameters}";
                    }
                }
                return field;
            }
        }

        public static TypeDefinition FromSymbol(INamedTypeSymbol symbol)
        {
            var genericParameters = symbol.TypeParameters.Select(tp => tp.Name).ToArray();
            BaseDefinition? parent = null;
            if (symbol.ContainingType is not null)
            {
                parent = FromSymbol(symbol.ContainingType);
            }
            else if (symbol.ContainingNamespace is not null && !symbol.ContainingNamespace.IsGlobalNamespace)
            {
                parent = NamespaceDefinition.FromSymbol(symbol.ContainingNamespace);
            }
            return new TypeDefinition(symbol.Name, parent, genericParameters);
        }
    }
}
