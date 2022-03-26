using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Stride.Core.Serialization;

namespace Stride.Core.CompilerServices
{
    public partial class SerializerGenerator
    {
        private const string DefaultProfile = "Default";
        private const string AssemblyProcessedAttribute = "Stride.Core.AssemblyProcessedAttribute";
        internal class Validator
        {
            private readonly INamedTypeSymbol enumSerializer;
            private readonly INamedTypeSymbol arraySerializer;
            private readonly GeneratorExecutionContext context;

            public Validator(GeneratorExecutionContext context)
            {
                this.context = context;
                enumSerializer = context.Compilation.GetTypeByMetadataName("Stride.Core.Serialization.Serializers.EnumSerializer`1");
                arraySerializer = context.Compilation.GetTypeByMetadataName("Stride.Core.Serialization.Serializers.ArraySerializer`1");
            }

            public void Validate(SerializerSpec serializerSpec)
            {
                CollectSerializableTypesFromDependencies(serializerSpec);

                GenerateGlobalSerializerAttributeSpecs(serializerSpec);
            }

            private void GenerateGlobalSerializerAttributeSpecs(SerializerSpec serializerSpec)
            {
                // For each type defined in this assembly add a registration
                foreach (var typeSpec in serializerSpec.DataContractTypes)
                {
                    var dataType = typeSpec.Type.IsGenericType ? typeSpec.Type.ConstructUnboundGenericType() : typeSpec.Type;
                    serializerSpec.GlobalSerializerRegistrationsToEmit.Add((dataType, DefaultProfile), new GlobalSerializerRegistration
                    {
                        DataType = dataType,
                        SerializerType = null,
                        Generated = true,
                        Inherited = typeSpec.Inherited,
                        GenericMode = typeSpec.Type.IsGenericType ? DataSerializerGenericMode.GenericArguments : DataSerializerGenericMode.None,
                    });
                }

                // TODO: iterate over registrations with serializerType == null && Generated == false and try to instatiate their serializers if dataType is closed generic

                foreach (var typeSpec in serializerSpec.DataContractTypes)
                {
                    var thisAssembly = typeSpec.Type.ContainingAssembly;

                    if (!ValidateBaseTypeChain(serializerSpec, typeSpec.BaseType, thisAssembly))
                    {
                        typeSpec.BaseType = null;
                    }

                    for (var i = 0; i < typeSpec.Members.Count; i++)
                    {
                        var member = typeSpec.Members[i];
                        // For each member validate it's serializable and for local generic instantiations add concrete serializer
                        if (!CheckTypeReference(serializerSpec, member.Type, thisAssembly))
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

                serializerSpec.GlobalSerializerRegistrationsToEmit = serializerSpec.GlobalSerializerRegistrationsToEmit;
            }

            private bool CheckTypeReference(SerializerSpec serializerSpec, ITypeSymbol typeSymbol, IAssemblySymbol assembly)
            {
                if (CheckMemberForSpecialKinds(serializerSpec, typeSymbol))
                {
                    return true;
                }

                if (ValidateBaseTypeChain(serializerSpec, typeSymbol as INamedTypeSymbol, assembly))
                {
                    return true;
                }

                return false;
            }

            /// <summary>
            /// True if member is special and has been handled, false otherwise.
            /// </summary>
            private bool CheckMemberForSpecialKinds(SerializerSpec serializerSpec, ITypeSymbol memberType)
            {
                if (memberType.TypeKind == TypeKind.Enum)
                {
                    if (!serializerSpec.GlobalSerializerRegistrationsToEmit.ContainsKey((memberType, DefaultProfile)) && !serializerSpec.DependencySerializerReference.ContainsKey((memberType, DefaultProfile)))
                    {
                        serializerSpec.GlobalSerializerRegistrationsToEmit.Add((memberType, DefaultProfile), new GlobalSerializerRegistration
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
                    if (!serializerSpec.GlobalSerializerRegistrationsToEmit.ContainsKey((memberType, DefaultProfile)) && !serializerSpec.DependencySerializerReference.ContainsKey((memberType, DefaultProfile)))
                    {
                        serializerSpec.GlobalSerializerRegistrationsToEmit.Add((memberType, DefaultProfile), new GlobalSerializerRegistration
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
                else if (memberType.TypeKind == TypeKind.Interface || (memberType.TypeKind == TypeKind.Class && memberType.IsAbstract))
                {
                    if (!serializerSpec.GlobalSerializerRegistrationsToEmit.ContainsKey((memberType, DefaultProfile)) && !serializerSpec.DependencySerializerReference.ContainsKey((memberType, DefaultProfile)))
                    {
                        serializerSpec.GlobalSerializerRegistrationsToEmit.Add((memberType, DefaultProfile), new GlobalSerializerRegistration
                        {
                            DataType = memberType,
                            SerializerType = null, // special case for abstract/interface types
                            Generated = false,
                            Inherited = false,
                            GenericMode = DataSerializerGenericMode.None,
                        });
                    }
                    return true;
                }
                // TODO: if nullable, verify NullableSerializer<T> is used

                return false;
            }

            /// <summary>
            /// True if validation was successful, false if base type should be made null.
            /// </summary>
            private bool ValidateBaseTypeChain(SerializerSpec serializerSpec, INamedTypeSymbol baseType, IAssemblySymbol thisAssembly)
            {
                while (baseType != null && !baseType.Equals(systemObjectSymbol, SymbolEqualityComparer.Default))
                {
                    // if it's from this assembly
                    if (baseType.ContainingAssembly.Equals(thisAssembly, SymbolEqualityComparer.Default))
                    {
                        if (serializerSpec.GlobalSerializerRegistrationsToEmit.ContainsKey((baseType, DefaultProfile)))
                        {
                            return true;
                        }
                        else
                        {
                            // If parent type is a closed generic type we need to add a registration for it
                            // TODO: see how AssemblyProcessor regards type arguments - IMO we shouldn't validate them to avoid unnecessary preventing stuff
                            if (baseType.TypeParameters.Length > 0)
                            {
                                var baseTypeDefinition = baseType.ConstructUnboundGenericType();
                                if (serializerSpec.GlobalSerializerRegistrationsToEmit.TryGetValue((baseTypeDefinition, DefaultProfile), out var registration))
                                {
                                    if (baseType.TypeArguments.All(static arg => arg.TypeKind != TypeKind.TypeParameter) &&
                                        !serializerSpec.GlobalSerializerRegistrationsToEmit.ContainsKey((baseType, DefaultProfile)))
                                    {
                                        serializerSpec.GlobalSerializerRegistrationsToEmit.Add((baseType, DefaultProfile), new GlobalSerializerRegistration
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
                        if (serializerSpec.DependencySerializerReference.ContainsKey((baseType, DefaultProfile)))
                        {
                            return true;
                        }
                        else
                        {
                            // If parent type is a closed generic type we need to add a registration for it
                            if (baseType.TypeParameters.Length > 0)
                            {
                                var baseTypeDefinition = baseType.ConstructUnboundGenericType();
                                if (serializerSpec.DependencySerializerReference.TryGetValue((baseTypeDefinition, DefaultProfile), out var registration))
                                {
                                    if (baseType.TypeArguments.All(static arg => arg.TypeKind != TypeKind.TypeParameter) &&
                                        !serializerSpec.GlobalSerializerRegistrationsToEmit.ContainsKey((baseType, DefaultProfile)))
                                    {
                                        serializerSpec.GlobalSerializerRegistrationsToEmit.Add((baseType, DefaultProfile), new GlobalSerializerRegistration
                                        {
                                            DataType = baseType,
                                            // using ConstructedFrom here to pass a ReferenceEquals check in Roslyn
                                            SerializerType = registration.SerializerType?.ConstructedFrom.Construct(baseType.TypeArguments, baseType.TypeArgumentNullableAnnotations),
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
            private void CollectSerializableTypesFromDependencies(SerializerSpec serializerSpec)
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

                    CollectSerializableTypesFromNamespaceRecursive(assembly.GlobalNamespace, serializerSpec);
                }
            }

            private void CollectSerializableTypesFromNamespaceRecursive(INamespaceSymbol @namespace, SerializerSpec serializerSpec) => VisitTypes(@namespace, type =>
            {
                if (!type.IsStatic)
                {
                    return;
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

                    if (!serializerSpec.DependencySerializerReference.ContainsKey((dataType, profile)))
                    {
                        serializerSpec.DependencySerializerReference.Add((dataType, profile), new GlobalSerializerRegistration
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
            });
        }
    }
}
