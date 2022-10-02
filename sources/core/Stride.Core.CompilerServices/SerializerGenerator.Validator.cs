using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Stride.Core.CompilerServices.Extensions;
using Stride.Core.CompilerServices.Models;
using Stride.Core.Serialization;
using static Stride.Core.CompilerServices.Diagnostics;

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
            private readonly GeneratorContext context;

            public Validator(GeneratorContext context)
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

                // try to instatiate serializers if null and dataType is closed generic
                foreach (var registrationKvp in serializerSpec.GlobalSerializerRegistrationsToEmit)
                {
                    var registration = registrationKvp.Value;
                    if (registration.SerializerType == null && registration.Generated == false)
                    {
                        if (!CheckTypeReference(serializerSpec, registration.DataType))
                        {
                            // TODO: complain - no serializer could be found for data type
                        }
                    }
                }

                foreach (var typeSpec in serializerSpec.DataContractTypes)
                {
                    if (!ValidateBaseTypeChain(serializerSpec, typeSpec.BaseType))
                    {
                        typeSpec.BaseType = null;
                    }

                    for (var i = 0; i < typeSpec.Members.Count; i++)
                    {
                        var member = typeSpec.Members[i];
                        // For each member validate it's serializable and for local generic instantiations add concrete serializer
                        if (!CheckTypeReference(serializerSpec, member.Type))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DataContractMemberHasNonSerializableType,
                                member.Member.Locations.FirstOrDefault(),
                                DiagnosticSeverity.Warning, // TODO: enable when switching to generator fully| member.HasExplicitDataMemberAttribute ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                                additionalLocations: null,
                                properties: null,
                                member.Name,
                                member.Member.ContainingType.ToStringSimpleClass(),
                                member.Type.ToStringSimpleClass()));

                            /* TODO uncomment once we emit the factory attributes fully
                            typeSpec.Members.RemoveAt(i);
                            i--;
                            */
                        }
                    }
                }

                serializerSpec.GlobalSerializerRegistrationsToEmit = serializerSpec.GlobalSerializerRegistrationsToEmit;
            }

            private bool CheckTypeReference(SerializerSpec serializerSpec, ITypeSymbol typeSymbol)
            {
                if (CheckMemberForSpecialKinds(serializerSpec, typeSymbol))
                {
                    return true;
                }

                if (ValidateBaseTypeChain(serializerSpec, typeSymbol as INamedTypeSymbol))
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
                            SerializerType = arraySerializer.Construct((memberType as IArrayTypeSymbol).ElementType),
                            Generated = false,
                            Inherited = false,
                            GenericMode = DataSerializerGenericMode.None,
                        });
                    }
                    return true;
                }
                else if (memberType.TypeKind == TypeKind.Interface || (memberType.TypeKind == TypeKind.Class && memberType.IsAbstract) || memberType.Is(context.WellKnownReferences.SystemObject))
                {
                    if (!serializerSpec.GlobalSerializerRegistrationsToEmit.ContainsKey((memberType, DefaultProfile)) && !serializerSpec.DependencySerializerReference.ContainsKey((memberType, DefaultProfile)))
                    {
                        serializerSpec.GlobalSerializerRegistrationsToEmit.Add((memberType, DefaultProfile), new GlobalSerializerRegistration
                        {
                            DataType = memberType,
                            SerializerType = null, // special case for abstract/interface types or System.Object
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
            private bool ValidateBaseTypeChain(SerializerSpec serializerSpec, INamedTypeSymbol baseType)
            {
                while (baseType != null && !baseType.Is(context.WellKnownReferences.SystemObject))
                {
                    bool? check = ValidateTypeInChain(serializerSpec, baseType);
                    if (check != null)
                    {
                        return check.Value;
                    }

                    baseType = baseType.BaseType;
                }

                return true;
            }
            
            private bool? ValidateTypeInChain(
                SerializerSpec serializerSpec,
                INamedTypeSymbol type)
            {
                // if the type is already known
                if (serializerSpec.GlobalSerializerRegistrationsToEmit.ContainsKey((type, DefaultProfile))
                    || serializerSpec.DependencySerializerReference.ContainsKey((type, DefaultProfile)))
                {
                    return true;
                }
                else
                {
                    // TODO: if base has a serializer with generic mode Type or TypeAndArguments
                    //       we need to copy it's serializer and instantiate it with this class.

                    // TODO: support for EnumerateGenericInstantiations method detection...
                    //       so the way AssemblyProcessor detects if it needs to process generic type arguments
                    //       is by reading an EnumerateGenericInstantiations method and finding ldtoken opcodes
                    //       to resolve the types added to the `genericInstantiations` list and validate them
                    //       Honestly, I don't know if inspecting methods is possible from roslyn, so we may need to change this with some hacks

                    // If type is a closed generic type we need to add a registration for it
                    // We will check if we have a registrations for its open type
                    if (type.TypeParameters.Length > 0)
                    {
                        var baseTypeDefinition = type.ConstructUnboundGenericType();
                        GlobalSerializerRegistration registration;
                        if (serializerSpec.GlobalSerializerRegistrationsToEmit.TryGetValue((baseTypeDefinition, DefaultProfile), out registration)
                            || serializerSpec.DependencySerializerReference.TryGetValue((baseTypeDefinition, DefaultProfile), out registration))
                        {
                            // ensure the type is not partially open
                            if (type.TypeArguments.All(static arg => arg.TypeKind != TypeKind.TypeParameter))
                            {
                                INamedTypeSymbol serializerType;
                                switch (registration.GenericMode)
                                {
                                    case DataSerializerGenericMode.None:
                                        // serializer is not generic, but type is generic
                                        serializerType = registration.SerializerType;
                                        break;
                                    case DataSerializerGenericMode.Type:
                                        // using ConstructedFrom here to pass a ReferenceEquals check in Roslyn for externally resolved types
                                        serializerType = registration.SerializerType?.ConstructedFrom.Construct(type);
                                        break;
                                    case DataSerializerGenericMode.GenericArguments:
                                        // using ConstructedFrom here to pass a ReferenceEquals check in Roslyn for externally resolved types
                                        serializerType = registration.SerializerType?.ConstructedFrom.Construct(type.TypeArguments, type.TypeArgumentNullableAnnotations);
                                        break;
                                    case DataSerializerGenericMode.TypeAndGenericArguments:
                                        // using ConstructedFrom here to pass a ReferenceEquals check in Roslyn for externally resolved types
                                        serializerType = registration.SerializerType?.ConstructedFrom.Construct(new[] {type}.Concat(type.TypeArguments).ToArray());
                                        break;
                                    default:
                                        return false; // default case for compiler complaining about uninitialized var, should not happen
                                }
                                serializerSpec.GlobalSerializerRegistrationsToEmit.Add((type, DefaultProfile), new GlobalSerializerRegistration
                                {
                                    DataType = type,
                                    SerializerType = serializerType,
                                    Generated = registration.Generated,
                                    Inherited = false,
                                    GenericMode = DataSerializerGenericMode.None,
                                });
                            }

                            // a serializer has been added or type has a mix of bound and unbound type parameters, anyways continue looking through the base chain
                            return null;
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
                    var serializerFactoryAttribute = assembly.GetAttributes().FirstOrDefault(attr => attr.AttributeClass.Is(context.WellKnownReferences.AssemblySerializerFactoryAttribute));
                    if (serializerFactoryAttribute == null)
                    {
                        continue;
                    }

                    CollectSerializableTypesFromSerializerFactory(serializerFactoryAttribute, serializerSpec);
                }
            }

            private void CollectSerializableTypesFromSerializerFactory(AttributeData serializerFactoryAttribute, SerializerSpec serializerSpec)
            {
                if (serializerFactoryAttribute.NamedArguments.Length == 0)
                {
                    // malformed attribute
                    return;
                }

                var type = serializerFactoryAttribute.NamedArguments[0].Value.Value as ITypeSymbol;

                foreach (var attribute in type.GetAttributes())
                {
                    if (!attribute.AttributeClass.Is(context.WellKnownReferences.DataSerializerGlobalAttribute))
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
            }
        }
    }
}
