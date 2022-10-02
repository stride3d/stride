using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Extensions;
using Stride.Core.CompilerServices.Models;
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

            var typeSpecs = new HashSet<SerializerTypeSpec>(new SerializerTypeSpec.EqualityComparer());
            var customSerializers = new ProfiledDictionary<ITypeSymbol, GlobalSerializerRegistration>();
            var inheritedCustomSerializableTypes = new HashSet<ITypeSymbol>();
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
                    customSerializers.Add(typeSymbol, new GlobalSerializerRegistration
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
                    var ctor = typeSymbol.Constructors.FirstOrDefault(ctor => !ctor.Parameters.Any()
                        && (ctor.DeclaredAccessibility == Accessibility.Public || ctor.DeclaredAccessibility == Accessibility.Internal));
                    if (!typeSymbol.IsValueType && !typeSymbol.IsAbstract && ctor == null)
                    {
                        // DataContract classes should have a public parameterless constructor to satisfy a generic new() constraint
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataContractClassHasNoAccessibleParameterlessCtor,
                            typeSymbol.Locations.FirstOrDefault(),
                            typeSymbol.ToStringSimpleClass()));
                        continue;
                    }

                    var spec = SerializerTypeSpecGenerator.GenerateTypeSpec(context, typeSymbol);
                    spec.HasInternalContructor = ctor?.DeclaredAccessibility == Accessibility.Internal;

                    typeSpecs.Add(spec);
                }


                CheckTypeForCustomSerializers(context, typeSymbol, customSerializers, inheritedCustomSerializableTypes);
            }

            // look for assembly scoped global serializers
            CheckTypeForCustomSerializers(context, assembly, customSerializers, inheritedCustomSerializableTypes);

            return new SerializerSpec
            {
                DataContractTypes = typeSpecs,
                Assembly = context.Compilation.Assembly,
                AllTypes = allTypes,
                GlobalSerializerRegistrationsToEmit = customSerializers,
                DependencySerializerReference = new ProfiledDictionary<ITypeSymbol, GlobalSerializerRegistration>(),
                InheritedCustomSerializableTypes = inheritedCustomSerializableTypes,
            };
        }

        private static List<INamedTypeSymbol> GetAllTypesForAssembly(IAssemblySymbol assembly)
        {
            var types = new List<INamedTypeSymbol>();
            assembly.GlobalNamespace.VisitTypes(type =>
            {
                types.Add(type);
                type.VisitNestedTypes(t =>
                {
                    if (t.DeclaredAccessibility == Accessibility.Public || t.DeclaredAccessibility == Accessibility.Internal)
                    {
                        types.Add(t);
                    }
                });
            });
            return types;
        }

        internal static void CheckTypeForCustomSerializers(GeneratorContext context, ISymbol symbol, ProfiledDictionary<ITypeSymbol, GlobalSerializerRegistration> customSerializers, HashSet<ITypeSymbol> inheritedCustomSerializableTypes)
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
                            attribute.ApplicationSyntaxReference.ToLocation()));

                        continue;
                    }

                    // if basetype is null we need to access the original definition, but this makes us loose generic arguments
                    // so we're reapplying them here to have a full type.
                    if (serializerType.BaseType == null)
                    {
                        serializerType = serializerType.GetFullTypeInfo();
                    }

                    spec = new GlobalSerializerRegistration
                    {
                        DataType = dataType,
                        SerializerType = serializerType,
                        GenericMode = genericMode,
                        Inherited = genericMode == DataSerializerGenericMode.Type
                            || (genericMode == DataSerializerGenericMode.TypeAndGenericArguments
                                && dataType is INamedTypeSymbol named && named.IsGenericInstance()),
                        AttributeLocation = attribute.ApplicationSyntaxReference.ToLocation(),
                    };
                }
                else if (attribute.AttributeClass.Is(context.WellKnownReferences.DataSerializerGlobalAttribute))
                {
                    var dataType = attribute.ConstructorArguments[1].Value as ITypeSymbol;
                    var serializerType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                    var genericMode = (DataSerializerGenericMode)(int)attribute.ConstructorArguments[2].Value;
                    var inherited = (bool)attribute.ConstructorArguments[3].Value;
                    var profile = attribute.NamedArguments.Length > 0 ? (string)attribute.NamedArguments[0].Value.Value : null;
                    
                    // if basetype is null we need to access the original definition, but this makes us loose generic arguments
                    // so we're reapplying them here to have a full type.
                    if (serializerType != null && serializerType.BaseType == null)
                    {
                        serializerType = serializerType.GetFullTypeInfo();
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
                        Inherited = inherited,
                        AttributeLocation = attribute.ApplicationSyntaxReference.ToLocation(),
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

                    if (customSerializers.ContainsKey(spec.DataType, spec.Profile))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataSerializerGlobalDuplicateDeclarations,
                            Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span),
                            spec.DataType.ToStringSimpleClass(),
                            spec.Profile));
                    }
                    else
                    {
                        customSerializers.Add(spec.DataType, spec, spec.Profile);

                        if (spec.Inherited)
                        {
                            inheritedCustomSerializableTypes.Add(spec.DataType as INamedTypeSymbol);
                        }
                    }
                }
            }
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
    }
}
