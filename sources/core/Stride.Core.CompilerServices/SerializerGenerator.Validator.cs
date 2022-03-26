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
                else if (memberType.TypeKind == TypeKind.Interface || (memberType.TypeKind == TypeKind.Class && memberType.IsAbstract) || memberType.Equals(systemObjectSymbol, SymbolEqualityComparer.Default))
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
                while (baseType != null && !baseType.Equals(systemObjectSymbol, SymbolEqualityComparer.Default))
                {
                    var referencesDictionary = serializerSpec.GlobalSerializerRegistrationsToEmit;
                    bool? check = ValidateTypeInChain(serializerSpec, referencesDictionary, baseType);
                    if (check == true)
                    {
                        return true;
                    }
                    else if (check == false)
                    {
                        referencesDictionary = serializerSpec.DependencySerializerReference;
                        check = ValidateTypeInChain(serializerSpec, referencesDictionary, baseType);
                        if (check != null)
                        {
                            return check.Value;
                        }
                    }

                    baseType = baseType.BaseType;
                }

                return true;
            }

            
            private bool? ValidateTypeInChain(
                SerializerSpec serializerSpec,
                Dictionary<(ITypeSymbol, string profile), GlobalSerializerRegistration>  referencesDictionary,
                INamedTypeSymbol type)
            {
                if (referencesDictionary.ContainsKey((type, DefaultProfile)))
                {
                    return true;
                }
                else
                {
                    // If parent type is a closed generic type we need to add a registration for it
                    // TODO: see how AssemblyProcessor regards type arguments - IMO we shouldn't validate them to avoid unnecessary preventing stuff
                    //       however, when type is known to expect a serializer of the argument and it isn't serializable we should return false
                    if (type.TypeParameters.Length > 0)
                    {
                        var baseTypeDefinition = type.ConstructUnboundGenericType();
                        if (referencesDictionary.TryGetValue((baseTypeDefinition, DefaultProfile), out var registration))
                        {
                            if (type.TypeArguments.All(static arg => arg.TypeKind != TypeKind.TypeParameter) &&
                                !serializerSpec.GlobalSerializerRegistrationsToEmit.ContainsKey((type, DefaultProfile)))
                            {
                                serializerSpec.GlobalSerializerRegistrationsToEmit.Add((type, DefaultProfile), new GlobalSerializerRegistration
                                {
                                    DataType = type,
                                    // using ConstructedFrom here to pass a ReferenceEquals check in Roslyn for externally resolved types
                                    SerializerType = registration.SerializerType?.ConstructedFrom.Construct(type.TypeArguments, type.TypeArgumentNullableAnnotations),
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
