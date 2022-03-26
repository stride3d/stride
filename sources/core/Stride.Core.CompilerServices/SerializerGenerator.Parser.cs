using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Stride.Core.Serialization;

namespace Stride.Core.CompilerServices
{
    public partial class SerializerGenerator
    {
        private const string DataContractAttributeName = "Stride.Core.DataContractAttribute";
        private const string DataSerializerAttributeName = "Stride.Core.Serialization.DataSerializerAttribute";
        private const string DataAliasAttributeName = "Stride.Core.DataAliasAttribute";
        private const string DataMemberAttributeName = "Stride.Core.DataMemberAttribute";
        private const string DataMemberIgnoreAttributeName = "Stride.Core.DataMemberIgnoreAttribute";
        private const string DataSerializerGlobalAttributeName = "Stride.Core.Serialization.DataSerializerGlobalAttribute";
        private const string StructLayoutAttributeName = "System.Runtime.InteropServices.StructLayoutAttribute";
        private const string FieldOffsetAttributeName = "System.Runtime.InteropServices.FieldOffsetAttribute";
        private const string DataSerializerName = "Stride.Core.Serialization.DataSerializer";

        private static readonly SymbolDisplayFormat SimpleClassNameWithNestedInfo = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

        internal static SerializerSpec GenerateSpec(GeneratorExecutionContext context)
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

                if (CheckForDataContract(typeSymbol))
                {
                    if (!typeSymbol.IsValueType && !typeSymbol.IsAbstract && !typeSymbol.Constructors.Any(
                            ctor => !ctor.Parameters.Any() && ctor.DeclaredAccessibility == Accessibility.Public))
                    {
                        // DataContract classes should have a public parameterless constructor to satisfy a generic new() constraint
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataContractClassHasNoAccessibleParameterlessCtor,
                            typeSymbol.Locations.FirstOrDefault(),
                            typeSymbol.ToDisplayString(SimpleClassNameWithNestedInfo)));
                        continue;
                    }

                    typeSpecs.Add(GenerateTypeSpec(context, typeSymbol));
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
            VisitTypes(assembly.GlobalNamespace, type => types.Add(type));
            return types;
        }

        private static SerializerTypeSpec GenerateTypeSpec(GeneratorExecutionContext context, INamedTypeSymbol type)
        {
            var typeAttributes = type.GetAttributes();
            var dataContractAttribute = typeAttributes.FirstOrDefault(static attr => attr.AttributeClass.ToDisplayString() == DataContractAttributeName);
            var dataAliasAttributes = typeAttributes.Where(static attr => attr.AttributeClass.ToDisplayString() == DataAliasAttributeName).ToList();

            var members = new List<SerializerMemberSpec>();
            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic || member is IMethodSymbol)
                    continue;

                var attributes = member.GetAttributes();
                var ignoreAttribute = attributes.FirstOrDefault(static attr => attr.AttributeClass.ToDisplayString() == DataMemberIgnoreAttributeName);
                var dataMemberAttribute = attributes.FirstOrDefault(static attr => attr.AttributeClass.ToDisplayString() == DataMemberAttributeName);

                if (ignoreAttribute != null)
                {
                    if (dataMemberAttribute != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataContractMemberHasBothIncludeAndIgnoreAttr,
                            member.Locations.FirstOrDefault(),
                            member.Name,
                            type.ToDisplayString(SimpleClassNameWithNestedInfo)));
                    }

                    // member was ignored
                    continue;
                }

                if (member.DeclaredAccessibility != Accessibility.Public)
                {
                    if (!(member.DeclaredAccessibility == Accessibility.Internal && dataMemberAttribute != null))
                    {
                        // member is not public or internal with attribute 
                        continue;
                    }
                }

                ITypeSymbol memberType;
                MemberAccessMode accessMode;

                if (member is IPropertySymbol prop)
                {
                    if (prop.SetMethod == null && prop.Type.IsValueType)
                    {
                        // structs have to be assignable to properties
                        continue;
                    }

                    if (prop.SetMethod != null && !(prop.SetMethod.DeclaredAccessibility == Accessibility.Public || prop.SetMethod.DeclaredAccessibility == Accessibility.Internal))
                    {
                        // setter is inaccessible
                        continue;
                    }

                    if (prop.GetMethod == null)
                    {
                        // a serializable property has to have a get method
                        continue;
                    }

                    if (prop.GetMethod.Parameters.Length > 0)
                    {
                        // Ignore properties with indexer
                        continue;
                    }

                    memberType = prop.Type;

                    accessMode = MemberAccessMode.ByLocalRef;
                    if (prop.SetMethod != null)
                    {
                        accessMode |= MemberAccessMode.WithAssignment;
                    }
                }
                else if (member is IFieldSymbol field)
                {
                    memberType = field.Type;
                    accessMode = field.IsReadOnly ? MemberAccessMode.ByLocalRef : MemberAccessMode.ByRef;
                }
                else
                {
                    // member is neither a property or a field?
                    continue;
                }

                int? order = GetOrderOfMember(typeAttributes, attributes, dataMemberAttribute);

                members.Add(new SerializerMemberSpec(member, memberType, order, accessMode));
            }

            // Sort members by their order
            members.Sort();

            var typeSpec = new SerializerTypeSpec(type, members)
            {
                Inherited = dataContractAttribute == null || (dataContractAttribute.NamedArguments.FirstOrDefault(static kvp => kvp.Key == "Inherited").Value.Value?.Equals(true) ?? false),
            };

            // add aliases
            {
                if (dataContractAttribute != null && dataContractAttribute.ConstructorArguments.FirstOrDefault() is TypedConstant aliasWrapper && aliasWrapper.Value is string alias)
                {
                    typeSpec.Aliases.Add(alias);
                }
            }
            foreach (var aliasAttr in dataAliasAttributes)
            {
                if (aliasAttr.ConstructorArguments.FirstOrDefault() is TypedConstant aliasWrapper && aliasWrapper.Value is string alias)
                {
                    typeSpec.Aliases.Add(alias);
                }
            }

            // Check if there's a base type, which will be validated later
            if (!type.BaseType.Equals(systemObjectSymbol, SymbolEqualityComparer.Default))
            {
                typeSpec.BaseType = type.BaseType;
            }

            return typeSpec;
        }

        private static int? GetOrderOfMember(ImmutableArray<AttributeData> typeAttributes, ImmutableArray<AttributeData> memberAttributes, AttributeData dataMemberAttribute)
        {
            var layoutAttribute = typeAttributes.FirstOrDefault(static attr => attr.AttributeClass.ToDisplayString() == StructLayoutAttributeName);
            if (layoutAttribute != null)
            {
                LayoutKind layoutKind = (LayoutKind)layoutAttribute.ConstructorArguments[0].Value;

                if (layoutKind == LayoutKind.Sequential)
                {
                    return null;
                }
                else if (layoutKind == LayoutKind.Explicit)
                {
                    var fieldOffsetAttribute = memberAttributes.FirstOrDefault(static attr => attr.AttributeClass.ToDisplayString() == FieldOffsetAttributeName);
                    int offset = (int)fieldOffsetAttribute.ConstructorArguments[0].Value;
                    return offset;
                }
            }

            if (dataMemberAttribute != null)
            {
                if (dataMemberAttribute.AttributeConstructor.Parameters.FirstOrDefault() is IParameterSymbol parameter &&
                    parameter.Type.Equals(systemInt32Symbol, SymbolEqualityComparer.Default))
                {
                    return (int)dataMemberAttribute.ConstructorArguments[0].Value;
                }
            }

            return null;
        }

        internal static void CheckTypeForCustomSerializers(GeneratorExecutionContext context, ISymbol symbol, Dictionary<(ITypeSymbol, string profile), GlobalSerializerRegistration> customSerializers)
        {
            var attributes = symbol.GetAttributes();
            foreach (var attribute in attributes)
            {
                GlobalSerializerRegistration spec = null;
                var attributeFullName = attribute.AttributeClass.ToDisplayString();
                if (attributeFullName == DataSerializerAttributeName)
                {
                    var dataType = symbol as ITypeSymbol;
                    var serializerType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                    var genericMode = attribute.NamedArguments.Length > 0 ? (DataSerializerGenericMode)(int)attribute.NamedArguments[0].Value.Value : DataSerializerGenericMode.None;
                    
                    spec = new GlobalSerializerRegistration
                    {
                        DataType = dataType,
                        SerializerType = serializerType,
                        GenericMode = genericMode,
                    };
                }
                else if (attributeFullName == DataSerializerGlobalAttributeName)
                {
                    var dataType = attribute.ConstructorArguments[1].Value as ITypeSymbol;
                    var serializerType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                    var genericMode = (DataSerializerGenericMode)(int)attribute.ConstructorArguments[2].Value;
                    var profile = attribute.NamedArguments.Length > 0 ? (string)attribute.NamedArguments[0].Value.Value : null;
                    
                    if (dataType == null && serializerType != null)
                    {
                        // TODO: I moved .OriginalDefinition for the later base type check, but do we need it here?
                        //       if base type is null then yes, but if type is generic then we will loose bound type arguments
                        //       ~> maybe serializerType.OriginalDefinition.Construct(serializerType.TypeArguments)
                        // we need to figure out the type from generic argument of DataSerializer`1 that serializerType extends
                        var baseType = serializerType.BaseType;
                        while (baseType != null)
                        {
                            if (baseType.ToDisplayString(NamespaceWithTypeNameWithoutGenerics) == DataSerializerName && baseType.IsGenericType)
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
                // TODO: content serializer

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
                        !HasBaseTypesMatchingPredicate(spec.SerializerType.OriginalDefinition, baseType => baseType.ToDisplayString() == DataSerializerName))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataSerializerDoesNotExtendDataSerializerBaseClass,
                            Location.Create(attribute.ApplicationSyntaxReference.SyntaxTree, attribute.ApplicationSyntaxReference.Span),
                            spec.SerializerType.ToDisplayString(SimpleClassNameWithNestedInfo)));
                        
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
                            spec.DataType.ToDisplayString(SimpleClassNameWithNestedInfo),
                            spec.Profile));
                    }
                    else
                    {
                        customSerializers.Add((spec.DataType, spec.Profile), spec);
                    }
                }
            }
        }

        private static bool CheckForDataContract(INamedTypeSymbol typeSymbol)
        {
            if (CheckSymbolClassAttributesForSpecificAttribute(typeSymbol, DataSerializerAttributeName))
            {
                // if a class has a custom serializer define we should not attempt to generate one for it from a DataContract
                return false;
            }
            
            // check if class has [DataContract]
            bool hasDataContract = CheckSymbolClassAttributesForSpecificAttribute(typeSymbol, DataContractAttributeName);

            // check if class inherits [DataContract]
            if (!hasDataContract)
            {
                hasDataContract = CheckSymbolBaseClassesForInheritedDataContract(typeSymbol);
            }

            return hasDataContract;
        }

        private static bool CheckSymbolClassAttributesForSpecificAttribute(INamedTypeSymbol classSymbol, string attributeClassName)
        {
            var attributes = classSymbol.GetAttributes();
            foreach (var attribute in attributes)
            {
                var attributeFullName = attribute.AttributeClass.ToDisplayString();
                if (attributeFullName == attributeClassName)
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

        private static bool CheckSymbolBaseClassesForInheritedDataContract(INamedTypeSymbol classSymbol)
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
                    var attributeFullName = attribute.AttributeClass.ToDisplayString();
                    if (attributeFullName == DataContractAttributeName)
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
    }
}
