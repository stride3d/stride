using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Stride.Core.Serialization;

namespace Stride.Core.CompilerServices
{
    public partial class SerializerGenerator
    {
        private const string AssemblyProcessedAttribute = "Stride.Core.AssemblyProcessedAttribute";
        internal class Validator
        {
            private readonly Dictionary<ITypeSymbol, GlobalSerializerRegistration> dependencyRegistrations = new Dictionary<ITypeSymbol, GlobalSerializerRegistration>();
            private INamedTypeSymbol enumSerializer;
            private INamedTypeSymbol arraySerializer;

            public void Validate(GeneratorExecutionContext context, SerializerSpec serializerSpec)
            {
                enumSerializer = context.Compilation.GetTypeByMetadataName("Stride.Core.Serialization.Serializers.EnumSerializer`1");
                arraySerializer = context.Compilation.GetTypeByMetadataName("Stride.Core.Serialization.Serializers.ArraySerializer`1");

                CollectSerializableTypesFromDependencies(context);
                serializerSpec.DependencySerializerReference = dependencyRegistrations;

                GenerateGlobalSerializerAttributeSpecs(context, serializerSpec);
            }

            private void GenerateGlobalSerializerAttributeSpecs(GeneratorExecutionContext context, SerializerSpec serializerSpec)
            {
                var registrations = new Dictionary<ITypeSymbol, GlobalSerializerRegistration>(SymbolEqualityComparer.Default);

                // TODO: collect explicit serializer attributes via DataSerializer and DataSerializerGlobal

                // For each type defined in this assembly add a registration
                foreach (var typeSpec in serializerSpec.DataContractTypes)
                {
                    var dataType = typeSpec.Type.IsGenericType ? typeSpec.Type.ConstructUnboundGenericType() : typeSpec.Type;
                    registrations.Add(dataType, new GlobalSerializerRegistration
                    {
                        DataType = dataType,
                        SerializerType = null,
                        Generated = true,
                        Inherited = typeSpec.Inherited,
                        GenericMode = typeSpec.Type.IsGenericType ? DataSerializerGenericMode.GenericArguments : DataSerializerGenericMode.None,
                    });
                }

                foreach (var typeSpec in serializerSpec.DataContractTypes)
                {
                    var thisAssembly = typeSpec.Type.ContainingAssembly;

                    if (!ValidateBaseTypeChain(registrations, typeSpec.BaseType, thisAssembly))
                    {
                        typeSpec.BaseType = null;
                    }

                    for (var i = 0; i < typeSpec.Members.Count; i++)
                    {
                        var member = typeSpec.Members[i];
                        var memberType = member.Type;
                        if (CheckMemberForSpecialKinds(registrations, member, memberType))
                        {
                            continue;
                        }
                        // TODO: if nullable, verify NullableSerializer<T> is used

                        // For each member validate it's serializable and for local generic instantiations add concrete serializer
                        if (!ValidateBaseTypeChain(registrations, member.Type as INamedTypeSymbol, thisAssembly))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DataContractMemberHasNonSerializableType,
                                member.Member.Locations.FirstOrDefault(),
                                member.Name,
                                member.Member.ContainingType.ToDisplayString(SimpleClassNameWithNestedInfo),
                                member.Type.ToDisplayString(SimpleClassNameWithNestedInfo)));

                            /* TODO uncomment once we emit the factory attributes fully
                            typeSpec.Members.RemoveAt(i);
                            i--;
                            */
                        }
                    }
                }

                serializerSpec.GlobalSerializerRegistrationsToEmit = registrations;
            }

            /// <summary>
            /// True if member is special and has been handled, false otherwise.
            /// </summary>
            private bool CheckMemberForSpecialKinds(Dictionary<ITypeSymbol, GlobalSerializerRegistration> registrations, SerializerMemberSpec member, ITypeSymbol memberType)
            {
                if (memberType.TypeKind == TypeKind.Enum)
                {
                    if (!registrations.ContainsKey(memberType) && !dependencyRegistrations.ContainsKey(memberType))
                    {
                        registrations.Add(memberType, new GlobalSerializerRegistration
                        {
                            DataType = memberType,
                            SerializerType = enumSerializer.Construct(memberType),
                            Generated = false,
                            Inherited = false,
                            GenericMode = DataSerializerGenericMode.None,
                        });
                    }
                    return true;
                }
                else if (memberType.TypeKind == TypeKind.Array)
                {
                    if (!registrations.ContainsKey(memberType) && !dependencyRegistrations.ContainsKey(memberType))
                    {
                        registrations.Add(member.Type, new GlobalSerializerRegistration
                        {
                            DataType = memberType,
                            SerializerType = arraySerializer.Construct(memberType),
                            Generated = false,
                            Inherited = false,
                            GenericMode = DataSerializerGenericMode.None,
                        });
                    }
                    return true;
                }

                return false;
            }

            /// <summary>
            /// True if validation was successful, false if base type should be made null.
            /// </summary>
            private bool ValidateBaseTypeChain(Dictionary<ITypeSymbol, GlobalSerializerRegistration> registrations, INamedTypeSymbol baseType, IAssemblySymbol thisAssembly)
            {
                while (baseType != null && !baseType.Equals(systemObjectSymbol, SymbolEqualityComparer.Default))
                {
                    // if it's from this assembly
                    if (baseType.ContainingAssembly.Equals(thisAssembly, SymbolEqualityComparer.Default))
                    {
                        if (registrations.ContainsKey(baseType))
                        {
                            return true;
                        }
                        else
                        {
                            // If parent type is a closed generic type we need to add a registration for it
                            if (baseType.TypeParameters.Length > 0)
                            {
                                var baseTypeDefinition = baseType.ConstructUnboundGenericType();
                                if (registrations.TryGetValue(baseTypeDefinition, out var registration))
                                {
                                    if (baseType.TypeArguments.All(static arg => arg.TypeKind != TypeKind.TypeParameter) &&
                                        !registrations.ContainsKey(baseType))
                                    {
                                        registrations.Add(baseType, new GlobalSerializerRegistration
                                        {
                                            DataType = baseType,
                                            SerializerType = registration.SerializerType?.Construct(baseType.TypeArguments, baseType.TypeArgumentNullableAnnotations),
                                            Generated = registration.Generated,
                                            Inherited = false,
                                            GenericMode = DataSerializerGenericMode.None,
                                        });
                                    }
                                }
                                else
                                {
                                    // the class is from the same assembly, but has no registered serializer
                                    return false;
                                }
                            }
                            else // not generic
                            {
                                // the class is from the same assembly, but has no registered serializer
                                return false;
                            }
                        }
                    }
                    else // from a dependency assembly
                    {
                        if (dependencyRegistrations.ContainsKey(baseType))
                        {
                            return true;
                        }
                        else
                        {
                            // If parent type is a closed generic type we need to add a registration for it
                            if (baseType.TypeParameters.Length > 0)
                            {
                                var baseTypeDefinition = baseType.ConstructUnboundGenericType();
                                if (dependencyRegistrations.TryGetValue(baseTypeDefinition, out var registration))
                                {
                                    if (baseType.TypeArguments.All(static arg => arg.TypeKind != TypeKind.TypeParameter) &&
                                        !registrations.ContainsKey(baseType))
                                    {
                                        registrations.Add(baseType, new GlobalSerializerRegistration
                                        {
                                            DataType = baseType,
                                            // using ConstructedFrom here to pass a ReferenceEquals check in Roslyn
                                            SerializerType = registration.SerializerType.ConstructedFrom.Construct(baseType.TypeArguments, baseType.TypeArgumentNullableAnnotations),
                                            Generated = registration.Generated,
                                            Inherited = false,
                                            GenericMode = DataSerializerGenericMode.None,
                                        });
                                    }
                                }
                                else
                                {
                                    // the class has no registered serializer
                                    return false;
                                }
                            }
                            else // not generic
                            {
                                // the class has no registered serializer
                                return false;
                            }
                        }
                    }

                    baseType = baseType.BaseType;
                }

                return true;
            }

            /// <summary>
            /// Look through attributes on static types to find serializer factories and collect data from [DataSerializerGlobal]
            /// </summary>
            private void CollectSerializableTypesFromDependencies(GeneratorExecutionContext context)
            {
                var assemblies = context.Compilation.References
                    .Select(context.Compilation.GetAssemblyOrModuleSymbol)
                    .OfType<IAssemblySymbol>()
                    .Cast<IAssemblySymbol>();

                foreach (var assembly in assemblies)
                {
                    // We only care about assemblies that have been processed by Stride's processor
                    // TODO: remove hack when emitting attribute
                    if (!assembly.Name.Contains("Stride") && assembly.GetTypeByMetadataName(AssemblyProcessedAttribute) == null)
                    {
                        continue;
                    }

                    CollectSerializableTypesFromNamespaceRecursive(assembly.GlobalNamespace);
                }
            }

            private void CollectSerializableTypesFromNamespaceRecursive(INamespaceSymbol @namespace)
            {
                foreach (var type in @namespace.GetTypeMembers())
                {
                    if (!type.IsStatic)
                    {
                        continue;
                    }

                    foreach (var attribute in type.GetAttributes())
                    {
                        if (attribute.AttributeClass.ToDisplayString() != DataSerializerGlobalAttributeName)
                        {
                            continue;
                        }

                        if (attribute.ConstructorArguments.Length != 5 || attribute.NamedArguments.Length != 1)
                        {
                            // not all of the data has been provided - this might not be on the factory.
                            continue;
                        }

                        var serializerType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
                        var dataType = attribute.ConstructorArguments[1].Value as ITypeSymbol;
                        var genericMode = (DataSerializerGenericMode)(int)attribute.ConstructorArguments[2].Value;
                        var inherited = (bool)attribute.ConstructorArguments[3].Value;
                        var generated = (bool)attribute.ConstructorArguments[4].Value;
                        var profile = (string)attribute.NamedArguments[0].Value.Value;

                        if (!dependencyRegistrations.ContainsKey(dataType))
                        {
                            dependencyRegistrations.Add(dataType, new GlobalSerializerRegistration
                            {
                                DataType = dataType,
                                SerializerType = serializerType,
                                Generated = generated,
                                Inherited = inherited,
                                GenericMode = genericMode,
                                Profile = profile,
                            });
                        }
                    }
                }

                foreach (var namespaceSymbol in @namespace.GetNamespaceMembers())
                {
                    CollectSerializableTypesFromNamespaceRecursive(namespaceSymbol);
                }
            }
        }
    }
}
