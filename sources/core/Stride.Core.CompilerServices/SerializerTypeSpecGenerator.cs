using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Extensions;
using static Stride.Core.CompilerServices.Diagnostics;

namespace Stride.Core.CompilerServices
{
    internal static class SerializerTypeSpecGenerator
    {
        public static SerializerTypeSpec GenerateTypeSpec(GeneratorContext context, INamedTypeSymbol type)
        {
            var typeAttributes = type.GetAttributes();
            var dataContractAttribute = typeAttributes.FirstOrDefault(attr => attr.AttributeClass.Is(context.WellKnownReferences.DataContractAttribute));
            var dataAliasAttributes = typeAttributes.Where(attr => attr.AttributeClass.Is(context.WellKnownReferences.DataAliasAttribute)).ToList();

            var members = new List<SerializerMemberSpec>();
            foreach (var member in type.GetMembers())
            {
                // TODO: currently AssemblyProcessor takes static members to generate serializers for them (i.e. PropertyKey)
                //       so we'd have to add a flag to remove it from member list after validating its type
                if (member.IsStatic || member is IMethodSymbol)
                    continue;

                var attributes = member.GetAttributes();
                var ignoreAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass.Is(context.WellKnownReferences.DataMemberIgnoreAttribute));
                var dataMemberAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass.Is(context.WellKnownReferences.DataMemberAttribute));

                if (ignoreAttribute != null)
                {
                    if (dataMemberAttribute != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DataContractMemberHasBothIncludeAndIgnoreAttr,
                            member.Locations.FirstOrDefault(),
                            member.Name,
                            type.ToStringSimpleClass()));
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

                int? order = GetOrderOfMember(context, typeAttributes, attributes, dataMemberAttribute);

                members.Add(new SerializerMemberSpec(member, memberType, order, accessMode, dataMemberAttribute != null));
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
            if (!type.BaseType.Equals(context.WellKnownReferences.SystemObject, SymbolEqualityComparer.Default))
            {
                typeSpec.BaseType = type.BaseType;
            }

            return typeSpec;
        }

        private static int? GetOrderOfMember(GeneratorContext context, ImmutableArray<AttributeData> typeAttributes, ImmutableArray<AttributeData> memberAttributes, AttributeData dataMemberAttribute)
        {
            var layoutAttribute = typeAttributes.FirstOrDefault(attr => attr.AttributeClass.Is(context.WellKnownReferences.InteropStructLayoutAttribute));
            if (layoutAttribute != null)
            {
                LayoutKind layoutKind = (LayoutKind)layoutAttribute.ConstructorArguments[0].Value;

                if (layoutKind == LayoutKind.Sequential)
                {
                    return null;
                }
                else if (layoutKind == LayoutKind.Explicit)
                {
                    var fieldOffsetAttribute = memberAttributes.FirstOrDefault(attr => attr.AttributeClass.Is(context.WellKnownReferences.InteropFieldOffsetAttribute));
                    int offset = (int)fieldOffsetAttribute.ConstructorArguments[0].Value;
                    return offset;
                }
            }

            if (dataMemberAttribute != null)
            {
                if (dataMemberAttribute.AttributeConstructor.Parameters.FirstOrDefault() is IParameterSymbol parameter &&
                    parameter.Type.Is(context.WellKnownReferences.SystemInt32))
                {
                    return (int)dataMemberAttribute.ConstructorArguments[0].Value;
                }
            }

            return null;
        }
    }
}
