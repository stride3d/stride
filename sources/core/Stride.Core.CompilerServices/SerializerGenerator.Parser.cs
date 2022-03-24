using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Core.CompilerServices
{
    public partial class SerializerGenerator
    {
        private const string DataContractAttributeName = "Stride.Core.DataContractAttribute";
        private const string DataSerializerAttributeName = "Stride.Core.Serialization.DataSerializerAttribute";
        private const string DataAliasAttributeName = "Stride.Core.DataAliasAttribute";
        private const string DataMemberAttributeName = "Stride.Core.DataMemberAttribute";
        private const string DataMemberIgnoreAttributeName = "Stride.Core.DataMemberIgnoreAttribute";
        private const string StructLayoutAttributeName = "System.Runtime.InteropServices.StructLayoutAttribute";
        private const string FieldOffsetAttributeName = "System.Runtime.InteropServices.FieldOffsetAttribute";

        private static readonly SymbolDisplayFormat SimpleClassNameWithNestedInfo = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

        internal static SerializerSpec GenerateSpec(GeneratorExecutionContext context)
        {
            HashSet<SerializerTypeSpec> typeSpecs = new HashSet<SerializerTypeSpec>(new SerializerTypeSpec.EqualityComparer());
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var semanticModel = context.Compilation.GetSemanticModel(syntaxTree);

                foreach (var typeSyntax in syntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Cast<TypeDeclarationSyntax>())
                {
                    if (typeSyntax is InterfaceDeclarationSyntax)
                    {
                        // we don't support interface serializers
                        continue;
                    }

                    var typeSymbol = semanticModel.GetDeclaredSymbol(typeSyntax) as INamedTypeSymbol;

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
                }
            }

            return new SerializerSpec
            {
                DataContractTypes = typeSpecs,
            };
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

                ITypeSymbol memberType = null;

                if (member is IPropertySymbol prop)
                {
                    if (prop.SetMethod == null && prop.Type.IsValueType)
                    {
                        // structs have to be assignable to properties
                        continue;
                    }

                    if (prop.GetMethod == null)
                    {
                        // a serializable property has to have a get method
                        continue;
                    }

                    memberType = prop.Type;
                }
                else if (member is IFieldSymbol field)
                {
                    memberType = field.Type;
                }

                if (memberType == null)
                {
                    // member is neither a property or a field?
                    continue;
                }

                int? order = GetOrderOfMember(typeAttributes, attributes, dataMemberAttribute);

                members.Add(new SerializerMemberSpec(member, memberType, order));
            }

            // Sort members by their order
            members.Sort();

            var typeSpec = new SerializerTypeSpec(type, members);

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
    }
}
