using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Extensions;
using Stride.Core.Serialization;
using static Stride.Core.CompilerServices.Diagnostics;

namespace Stride.Core.CompilerServices
{
    public partial class SerializerGenerator
    {
        internal static SerializerSpec GenerateSpec(GeneratorContext context)
        {
            var assembly = context.Compilation.Assembly;
            var allTypes = GetAllTypesForAssembly(assembly);

            HashSet<SerializerTypeSpec> typeSpecs = new HashSet<SerializerTypeSpec>(new SerializerTypeSpec.EqualityComparer());
            Dictionary<(ITypeSymbol, string), GlobalSerializerRegistration> customSerializers = new Dictionary<(ITypeSymbol, string), GlobalSerializerRegistration>();
            foreach (var typeSymbol in allTypes)
            {
                if (typeSymbol.DeclaredAccessibility != Accessibility.Public && typeSymbol.DeclaredAccessibility != Accessibility.Internal)
                {
                    // class is not accessible, we won't warn because it may be used for YAML reflection based serialization
                    continue;
                }

                if (typeSymbol.IsGenericType && typeSymbol.TypeParameters.Length == 0)
                {
                    // this is likely a nested type that extends a generic class which take a parameter from outer class
                    // TODO skipping for now 
                    continue;
                }

                if (typeSymbol.TypeKind == TypeKind.Enum)
                {
                    customSerializers.Add((typeSymbol, DefaultProfile), new GlobalSerializerRegistration
                    {
                        DataType = typeSymbol,
                        SerializerType = context.WellKnownReferences.EnumSerializer.Construct(typeSymbol),
                        Generated = false,
                        Inherited = false,
                        GenericMode = DataSerializerGenericMode.None,
                    });
                }
                else if (CheckForDataContract(context, typeSymbol))
                {
                    if (!typeSymbol.IsValueType && !typeSymbol.IsAbstract && !typeSymbol.Constructors.Any(
                            ctor => !ctor.Parameters.Any() && ctor.DeclaredAccessibility == Accessibility.Public))
                    {
                        // DataContract classes should have a public parameterless constructor to satisfy a generic new() constraint
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataContractClassHasNoAccessibleParameterlessCtor,
                            typeSymbol.Locations.FirstOrDefault(),
                            typeSymbol.ToStringSimpleClass()));
                        continue;
                    }

                    typeSpecs.Add(SerializerTypeSpecGenerator.GenerateTypeSpec(context, typeSymbol));
                }


                CheckTypeForCustomSerializers(context, typeSymbol, customSerializers);
            }

            // look for assembly scoped global serializers
            CheckTypeForCustomSerializers(context, assembly, customSerializers);

            return new SerializerSpec
            {
                DataContractTypes = typeSpecs,
                Assembly = context.Compilation.Assembly,
                GlobalSerializerRegistrationsToEmit = customSerializers,
                DependencySerializerReference = new Dictionary<(ITypeSymbol, string profile), GlobalSerializerRegistration>(),
            };
        }

        private static List<INamedTypeSymbol> GetAllTypesForAssembly(IAssemblySymbol assembly)
        {
            var types = new List<INamedTypeSymbol>();
            VisitTypes(assembly.GlobalNamespace, type =>
            {
                types.Add(type);
                VisitNestedTypes(type, t =>
                {
                    if (t.DeclaredAccessibility == Accessibility.Public || t.DeclaredAccessibility == Accessibility.Internal)
                    {
                        types.Add(t);
                    }
                });
            });
            return types;
        }

        internal static void CheckTypeForCustomSerializers(GeneratorContext context, ISymbol symbol, Dictionary<(ITypeSymbol, string profile), GlobalSerializerRegistration> customSerializers)
        {
            var attributes = symbol.GetAttributes();
            foreach (var attribute in attributes)
            {
                GlobalSerializerRegistration spec = null;
                if (attribute.AttributeClass.Is(context.WellKnownReferences.DataSerializerAttribute))
                {
                    var dataType = symbol as ITypeSymbol;
                    var serializerType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                    var genericMode = attribute.NamedArguments.Length > 0 ? (DataSerializerGenericMode)(int)attribute.NamedArguments[0].Value.Value : DataSerializerGenericMode.None;

                    if (serializerType == null)
                    {
                        // the compiler should have complained as attribute has a [NotNull] on the serializer type
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataSerializerNoTypeInformation,
                            Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span)));

                        continue;
                    }

                    // if basetype is null we need to access the original definition, but this makes us loose generic arguments
                    // so we're reapplying them here to have a full type.
                    if (serializerType.BaseType == null)
                    {
                        serializerType = GetFullTypeInfoFrom(serializerType);
                    }

                    spec = new GlobalSerializerRegistration
                    {
                        DataType = dataType,
                        SerializerType = serializerType,
                        GenericMode = genericMode,
                    };
                }
                else if (attribute.AttributeClass.Is(context.WellKnownReferences.DataSerializerGlobalAttribute))
                {
                    var dataType = attribute.ConstructorArguments[1].Value as ITypeSymbol;
                    var serializerType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                    var genericMode = (DataSerializerGenericMode)(int)attribute.ConstructorArguments[2].Value;
                    var profile = attribute.NamedArguments.Length > 0 ? (string)attribute.NamedArguments[0].Value.Value : null;
                    
                    // if basetype is null we need to access the original definition, but this makes us loose generic arguments
                    // so we're reapplying them here to have a full type.
                    if (serializerType != null && serializerType.BaseType == null)
                    {
                        serializerType = GetFullTypeInfoFrom(serializerType);
                    }

                    if (dataType == null && serializerType != null)
                    {
                        // we need to figure out the type from generic argument of DataSerializer`1 that serializerType extends
                        var baseType = serializerType.BaseType;
                        while (baseType != null)
                        {
                            if (baseType.IsGeneric(context.WellKnownReferences.DataSerializer))
                            {
                                dataType = baseType.TypeArguments[0];
                                break;
                            }
                            baseType = baseType.BaseType;
                        }
                    }

                    spec = new GlobalSerializerRegistration
                    {
                        DataType = dataType,
                        SerializerType = serializerType,
                        GenericMode = genericMode,
                    };

                    if (profile != null)
                    {
                        spec.Profile = profile;
                    }
                }

                if (spec != null)
                {
                    // we allow serializerType to be null if dataType:
                    //   is abstract
                    //   is interface
                    //   is System.Object
                    //   is closed generic and we can find a serializer for the open generic type to construct a specialization
                    if (spec.SerializerType == null && spec.DataType == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataSerializerGlobalNoTypeInformation,
                            Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span)));
                        
                        continue;
                    }
                    // get the OriginalDefinition for this symbol to access base type info
                    // and validate it extends Stride.Core.Serialization.DataSerializer
                    if (spec.SerializerType != null &&
                        !HasBaseTypesMatchingPredicate(spec.SerializerType, baseType => baseType.IsGeneric(context.WellKnownReferences.DataSerializer)))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataSerializerDoesNotExtendDataSerializerBaseClass,
                            Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span),
                            spec.SerializerType.ToStringSimpleClass()));
                        
                        continue;
                    }
                    

                    // if we're dealing with generic types we need to make sure they are unbound
                    if (spec.SerializerType != null && spec.SerializerType.TypeParameters.Length > 0 && spec.SerializerType.TypeArguments.All(static arg => arg.TypeKind == TypeKind.TypeParameter))
                    {
                        spec.SerializerType = spec.SerializerType.ConstructUnboundGenericType();
                    }
                    if (spec.DataType is INamedTypeSymbol namedDataType && namedDataType.TypeParameters.Length > 0 && namedDataType.TypeArguments.All(static arg => arg.TypeKind == TypeKind.TypeParameter))
                    {
                        spec.DataType = namedDataType.ConstructUnboundGenericType();
                    }

                    if (customSerializers.ContainsKey((spec.DataType, spec.Profile)))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataSerializerGlobalDuplicateDeclarations,
                            Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span),
                            spec.DataType.ToStringSimpleClass(),
                            spec.Profile));
                    }
                    else
                    {
                        customSerializers.Add((spec.DataType, spec.Profile), spec);
                    }
                }
            }
        }

        private static INamedTypeSymbol GetFullTypeInfoFrom(INamedTypeSymbol serializerType)
        {
            var original = serializerType.OriginalDefinition;
            if (serializerType.IsGenericType && !serializerType.IsUnboundGenericType && serializerType.TypeArguments.All(static arg => arg.TypeKind != TypeKind.TypeParameter))
            {
                return original.ConstructedFrom.Construct(serializerType.TypeArguments, serializerType.TypeArgumentNullableAnnotations);
            }
            return original;
        }

        private static bool CheckForDataContract(GeneratorContext context, INamedTypeSymbol typeSymbol)
        {
            if (CheckSymbolClassAttributesForSpecificAttribute(typeSymbol, context.WellKnownReferences.DataSerializerAttribute))
            {
                // if a class has a custom serializer define we should not attempt to generate one for it from a DataContract
                return false;
            }
            
            // check if class has [DataContract]
            bool hasDataContract = CheckSymbolClassAttributesForSpecificAttribute(typeSymbol, context.WellKnownReferences.DataContractAttribute);

            // check if class inherits [DataContract]
            if (!hasDataContract)
            {
                hasDataContract = CheckSymbolBaseClassesForInheritedDataContract(context, typeSymbol);
            }

            return hasDataContract;
        }

        private static bool CheckSymbolClassAttributesForSpecificAttribute(INamedTypeSymbol classSymbol, INamedTypeSymbol attributeClass)
        {
            var attributes = classSymbol.GetAttributes();
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass.Is(attributeClass))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasBaseTypesMatchingPredicate(INamedTypeSymbol type, Func<INamedTypeSymbol, bool> predicate)
        {
            var baseSymbol = type.BaseType;
            while (baseSymbol != null)
            {
                if (predicate(baseSymbol))
                {
                    return true;
                }

                baseSymbol = baseSymbol.BaseType;
            }

            return false;
        }

        private static bool CheckSymbolBaseClassesForInheritedDataContract(GeneratorContext context, INamedTypeSymbol classSymbol)
        {
            var baseSymbol = classSymbol.BaseType;
            while (baseSymbol != null)
            {
                var attributes = baseSymbol.GetAttributes();

                if (attributes == null)
                {
                    continue;
                }

                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeClass.Is(context.WellKnownReferences.DataContractAttribute))
                    {
                        foreach (var namedArgument in attribute.NamedArguments)
                        {
                            if (namedArgument.Key == "Inherited" && namedArgument.Value.Value.Equals(true))
                            {
                                return true;
                            }
                        }
                    }
                }

                baseSymbol = baseSymbol.BaseType;
            }

            return false;
        }

        private static void VisitTypes(INamespaceSymbol @namespace, Action<INamedTypeSymbol> visitor)
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

        private static void VisitNestedTypes(INamedTypeSymbol type, Action<INamedTypeSymbol> visitor)
        {
            foreach (var nestedType in type.GetMembers().OfType<INamedTypeSymbol>().Cast<INamedTypeSymbol>())
            {
                visitor(nestedType);
                VisitNestedTypes(nestedType, visitor);
            }
        }
    }
}
