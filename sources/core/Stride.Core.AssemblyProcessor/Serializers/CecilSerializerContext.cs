// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Core.AssemblyProcessor.Serializers
{
    internal class CecilSerializerContext
    {
        private readonly TextWriter log;

        public CecilSerializerContext(PlatformType platform, AssemblyDefinition assembly, TextWriter log)
        {
            Platform = platform;
            Assembly = assembly;
            SerializableTypesProfiles = new Dictionary<string, ProfileInfo>();
            SerializableTypes = new ProfileInfo();
            SerializableTypesProfiles.Add("Default", SerializableTypes);
            ComplexTypes = new Dictionary<TypeDefinition, SerializableTypeInfo>();
            this.log = log;

            StrideCoreModule = assembly.GetStrideCoreModule();
        }

        public PlatformType Platform { get; }

        /// <summary>
        /// Gets the assembly being processed.
        /// </summary>
        /// <value>
        /// The assembly being processed.
        /// </value>
        public AssemblyDefinition Assembly { get; private set; }

        public ModuleDefinition StrideCoreModule { get; private set; }

        public List<Tuple<string, TypeDefinition, bool>> DataContractAliases { get; } = new List<Tuple<string, TypeDefinition, bool>>();

        /// <summary>
        /// Gets the list of serializable type grouped by profile.
        /// </summary>
        /// <value>
        /// The serializable types profiles.
        /// </value>
        public Dictionary<string, ProfileInfo> SerializableTypesProfiles { get; private set; }

        /// <summary>
        /// Gets the set of type that can be serialized (key) with their serializer name (string), corresponding to the "Default" profile.
        /// </summary>
        public ProfileInfo SerializableTypes { get; private set; }

        /// <summary>
        /// Gets the list of complex serializers to generate.
        /// </summary>
        /// <value>
        /// The list of complex serializers to generate.
        /// </value>
        public Dictionary<TypeDefinition, SerializableTypeInfo> ComplexTypes { get; private set; }

        /// <summary>
        /// Ensure the following type can be serialized. If not, try to register appropriate serializer.
        /// This method can be recursive.
        /// </summary>
        /// <param name="type">The type.</param>
        public SerializableTypeInfo GenerateSerializer(TypeReference type, bool force = true, string profile = "Default", bool generic = false)
        {
            var serializableTypes = GetSerializableTypes(profile);

            // Already handled?
            SerializableTypeInfo serializableTypeInfo;
            if (serializableTypes.TryGetSerializableTypeInfo(type, generic, out serializableTypeInfo))
                return serializableTypeInfo;

            // Try to get one without generic
            if (generic && serializableTypes.TryGetSerializableTypeInfo(type, false, out serializableTypeInfo))
                return serializableTypeInfo;

            // TDOO: Array, List, Generic types, etc... (equivalent of previous serializer factories)
            var arrayType = type as ArrayType;
            if (arrayType != null)
            {
                // Only proceed if element type is serializable (and in Default profile, otherwise ElementType is enough)
                if (GenerateSerializer(arrayType.ElementType, force, profile) != null)
                {
                    if (profile == "Default")
                    {
                        var arraySerializerType = StrideCoreModule.GetTypeResolved("Stride.Core.Serialization.Serializers.ArraySerializer`1");
                        var serializerType = new GenericInstanceType(arraySerializerType);
                        serializerType.GenericArguments.Add(arrayType.ElementType);
                        AddSerializableType(type, serializableTypeInfo = new SerializableTypeInfo(serializerType, true), profile);
                        return serializableTypeInfo;
                    }
                    else
                    {
                        // Fallback to default
                        return GenerateSerializer(type, force, "Default");
                    }
                }

                return null;
            }

            // Try to match with existing generic serializer (for List, Dictionary, etc...)
            var genericInstanceType = type as GenericInstanceType;
            if (genericInstanceType != null)
            {
                var elementType = genericInstanceType.ElementType;
                SerializableTypeInfo elementSerializableTypeInfo;
                if ((elementSerializableTypeInfo = GenerateSerializer(elementType, false, profile, true)) != null)
                {
                    switch (elementSerializableTypeInfo.Mode)
                    {
                        case DataSerializerGenericMode.Type:
                        {
                            var serializerType = new GenericInstanceType(elementSerializableTypeInfo.SerializerType);
                            serializerType.GenericArguments.Add(type);

                            AddSerializableType(type, serializableTypeInfo = new SerializableTypeInfo(serializerType, true) { ComplexSerializer = elementSerializableTypeInfo.ComplexSerializer }, profile);
                            break;
                        }
                        case DataSerializerGenericMode.TypeAndGenericArguments:
                        {
                            var serializerType = new GenericInstanceType(elementSerializableTypeInfo.SerializerType);
                            serializerType.GenericArguments.Add(type);
                            foreach (var genericArgument in genericInstanceType.GenericArguments)
                            {
                                // Generate serializer for each generic argument
                                //GenerateSerializer(genericArgument);
            
                                serializerType.GenericArguments.Add(genericArgument);
                            }

                            AddSerializableType(type, serializableTypeInfo = new SerializableTypeInfo(serializerType, true) { ComplexSerializer = elementSerializableTypeInfo.ComplexSerializer }, profile);
                            break;
                        }
                        case DataSerializerGenericMode.GenericArguments:
                        {
                            var serializerType = new GenericInstanceType(elementSerializableTypeInfo.SerializerType);
                            foreach (var genericArgument in genericInstanceType.GenericArguments)
                            {
                                // Generate serializer for each generic argument
                                //GenerateSerializer(genericArgument);
            
                                serializerType.GenericArguments.Add(genericArgument);
                            }

                            AddSerializableType(type, serializableTypeInfo = new SerializableTypeInfo(serializerType, true) { ComplexSerializer = elementSerializableTypeInfo.ComplexSerializer }, profile);
                            break;
                        }
                        default:
                            throw new NotImplementedException();
                    }
            
                    if (elementSerializableTypeInfo.ComplexSerializer)
                    {
                        ProcessComplexSerializerMembers(type, serializableTypeInfo);
                    }
                    return serializableTypeInfo;
                }
            }

            // Check complex type definitions
            if (profile == "Default" && (serializableTypeInfo = FindSerializerInfo(type, generic)) != null)
            {
                return serializableTypeInfo;
            }

            // Fallback to default
            if (profile != "Default")
                return GenerateSerializer(type, force, "Default", generic);

            // Part after that is only if a serializer is absolutely necessary. This is skipped when scanning normal assemblies type that might have nothing to do with serialization.
            if (!force)
                return null;
            
            // Non instantiable type? (object, interfaces, abstract classes)
            // Serializer can be null since they will be inherited anyway (handled through MemberSerializer)
            var resolvedType = type.Resolve();
            if (resolvedType.IsAbstract || resolvedType.IsInterface || resolvedType.FullName == typeof(object).FullName)
            {
                AddSerializableType(type, serializableTypeInfo = new SerializableTypeInfo(null, true), profile);
                return serializableTypeInfo;
            }

            return null;
        }

        private void ProcessComplexSerializerMembers(TypeReference type, SerializableTypeInfo serializableTypeInfo, string profile = "Default")
        {
            // Process base type (for complex serializers)
            // If it's a closed type and there is a serializer, we'll serialize parent
            SerializableTypeInfo parentSerializableTypeInfo;
            var parentType = ResolveGenericsVisitor.Process(type, type.Resolve().BaseType);
            if (!parentType.ContainsGenericParameter() && (parentSerializableTypeInfo = GenerateSerializer(parentType, false, profile)) != null &&
                parentSerializableTypeInfo.SerializerType != null)
            {
                serializableTypeInfo.ComplexSerializerProcessParentType = true;
            }

            // Process members
            foreach (var serializableItem in ComplexSerializerRegistry.GetSerializableItems(type, true))
            {
                // Check that all closed types have a proper serializer
                if (serializableItem.Attributes.Any(x => x.AttributeType.FullName == "Stride.Core.DataMemberCustomSerializerAttribute")
                    || serializableItem.Type.ContainsGenericParameter())
                    continue;

                var resolvedType = serializableItem.Type.Resolve();
                var isInterface = resolvedType != null && resolvedType.IsInterface;

                try
                {
                    if (GenerateSerializer(serializableItem.Type, profile: profile) == null)
                    {
                        ComplexSerializerRegistry.IgnoreMember(serializableItem.MemberInfo);
                        if (!isInterface)
                            log.Write(
                                $"Warning: Member {serializableItem.MemberInfo} does not have a valid serializer. Add [DataMemberIgnore], turn the member non-public, or add a [DataContract] to it's type.");
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Could not process serialization for member {serializableItem.MemberInfo}", e);
                }
            }
        }

        /// <summary>
        /// Finds the serializer information.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="generic">If set to true, when using <see cref="DataSerializerGenericMode.Type"/>, it will returns the generic version instead of actual type one.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">Not sure how to process this inherited serializer</exception>
        internal SerializableTypeInfo FindSerializerInfo(TypeReference type, bool generic)
        {
            if (type == null || type.FullName == typeof(object).FullName || type.FullName == typeof(ValueType).FullName || type.IsGenericParameter)
                return null;

            var resolvedType = type.Resolve();

            // Nested type
            if (resolvedType.IsNested)
            {
                // Check public/private flags
                if (!resolvedType.IsNestedPublic && !resolvedType.IsNestedAssembly)
                    return null;
            }

            if (resolvedType.IsEnum)
            {
                // Enum
                // Let's generate a EnumSerializer
                var enumSerializerType = StrideCoreModule.GetTypeResolved("Stride.Core.Serialization.Serializers.EnumSerializer`1");
                var serializerType = new GenericInstanceType(enumSerializerType);
                serializerType.GenericArguments.Add(type);

                var serializableTypeInfo = new SerializableTypeInfo(serializerType, true, DataSerializerGenericMode.None);
                AddSerializableType(type, serializableTypeInfo);
                return serializableTypeInfo;
            }

            // 1. Check if there is a Serializable attribute
            // Note: Not anymore since we don't want all system types to have unknown complex serializers.
            //if (((resolvedType.Attributes & TypeAttributes.Serializable) == TypeAttributes.Serializable) || resolvedType.CustomAttributes.Any(x => x.AttributeType.FullName == typeof(SerializableAttribute).FullName))
            //{
            //    serializerInfo.Serializable = true;
            //    serializerInfo.ComplexSerializer = true;
            //    serializerInfo.ComplexSerializerName = SerializerTypeName(resolvedType);
            //    return serializerInfo;
            //}

            // 2.1. Check if there is DataSerializerAttribute on this type (if yes, it is serializable, but not a "complex type")
            var dataSerializerAttribute =
                resolvedType.CustomAttributes.FirstOrDefault(
                    x => x.AttributeType.FullName == "Stride.Core.Serialization.DataSerializerAttribute");
            if (dataSerializerAttribute != null)
            {
                var modeField = dataSerializerAttribute.Fields.FirstOrDefault(x => x.Name == "Mode");
                var mode = (modeField.Name != null) ? (DataSerializerGenericMode)modeField.Argument.Value : DataSerializerGenericMode.None;

                var dataSerializerType = ((TypeReference)dataSerializerAttribute.ConstructorArguments[0].Value);

                // Reading from custom arguments doesn't have its ValueType properly set
                dataSerializerType = dataSerializerType.FixupValueType();

                if (mode == DataSerializerGenericMode.Type || (mode == DataSerializerGenericMode.TypeAndGenericArguments && type is GenericInstanceType))
                {
                    var genericSerializableTypeInfo = new SerializableTypeInfo(dataSerializerType, true, mode) { Inherited = true };
                    AddSerializableType(type, genericSerializableTypeInfo);

                    var actualSerializerType = new GenericInstanceType(dataSerializerType);

                    // Add Type as generic arguments
                    actualSerializerType.GenericArguments.Add(type);

                    // If necessary, add generic arguments too
                    if (mode == DataSerializerGenericMode.TypeAndGenericArguments)
                    {
                        foreach (var genericArgument in ((GenericInstanceType)type).GenericArguments)
                        {
                            actualSerializerType.GenericArguments.Add(genericArgument);
                        }
                    }

                    // Special case for GenericMode == DataSerializerGenericMode.Type: we store actual serializer instantiation in SerializerType (alongside the generic version in GenericSerializerType).
                    var serializableTypeInfo = new SerializableTypeInfo(actualSerializerType, true);
                    AddSerializableType(type, serializableTypeInfo);

                    if (!generic)
                        return serializableTypeInfo;

                    return genericSerializableTypeInfo;
                }
                else
                {
                    var serializableTypeInfo = new SerializableTypeInfo(dataSerializerType, true, mode) { Inherited = false };
                    AddSerializableType(type, serializableTypeInfo);
                    return serializableTypeInfo;
                }
            }

            // 2.2. Check if SerializableExtendedAttribute is set on this class, or any of its base class with ApplyHierarchy
            var serializableExtendedAttribute =
                resolvedType.CustomAttributes.FirstOrDefault(
                    x => x.AttributeType.FullName == "Stride.Core.DataContractAttribute");
            if (dataSerializerAttribute == null && serializableExtendedAttribute != null)
            {
                // CHeck if ApplyHierarchy is active, otherwise it needs to be the exact type.
                var inherited = serializableExtendedAttribute.Properties.Where(x => x.Name == "Inherited")
                    .Select(x => (bool)x.Argument.Value)
                    .FirstOrDefault();

                var serializableTypeInfo = CreateComplexSerializer(type);
                serializableTypeInfo.Inherited = inherited;

                // Process members
                ProcessComplexSerializerMembers(type, serializableTypeInfo);

                return serializableTypeInfo;
            }

            // Check if parent type contains Inherited attribute
            var parentType = ResolveGenericsVisitor.Process(type, type.Resolve().BaseType);
            if (parentType != null)
            {
                // Generate serializer for parent type
                var parentSerializableInfoType = GenerateSerializer(parentType.Resolve(), false, generic: true);

                // If Inherited flag is on, we also generate a serializer for this type
                if (parentSerializableInfoType != null && parentSerializableInfoType.Inherited)
                {
                    if (parentSerializableInfoType.ComplexSerializer)
                    {
                        var serializableTypeInfo = CreateComplexSerializer(type);
                        serializableTypeInfo.Inherited = true;

                        // Process members
                        ProcessComplexSerializerMembers(type, serializableTypeInfo);

                        return serializableTypeInfo;
                    }
                    else if (parentSerializableInfoType.Mode == DataSerializerGenericMode.Type || parentSerializableInfoType.Mode == DataSerializerGenericMode.TypeAndGenericArguments)
                    {
                        // Register generic version
                        var genericSerializableTypeInfo = new SerializableTypeInfo(parentSerializableInfoType.SerializerType, true, parentSerializableInfoType.Mode);
                        AddSerializableType(type, genericSerializableTypeInfo);

                        if (!type.HasGenericParameters)
                        {
                            var actualSerializerType = new GenericInstanceType(parentSerializableInfoType.SerializerType);

                            // Add Type as generic arguments
                            actualSerializerType.GenericArguments.Add(type);

                            // If necessary, add generic arguments too
                            if (parentSerializableInfoType.Mode == DataSerializerGenericMode.TypeAndGenericArguments)
                            {
                                foreach (var genericArgument in ((GenericInstanceType)parentType).GenericArguments)
                                {
                                    actualSerializerType.GenericArguments.Add(genericArgument);
                                }
                            }

                            // Register actual type
                            var serializableTypeInfo = new SerializableTypeInfo(actualSerializerType, true);
                            AddSerializableType(type, serializableTypeInfo);

                            if (!generic)
                                return serializableTypeInfo;
                        }

                        return genericSerializableTypeInfo;
                    }
                    else
                    {
                        throw new InvalidOperationException("Not sure how to process this inherited serializer");
                    }
                }
            }

            return null;
        }

        private SerializableTypeInfo CreateComplexSerializer(TypeReference type)
        {
            var isLocal = type.Resolve().Module.Assembly == Assembly;

            // Create a fake TypeReference (even though it doesn't really exist yet, but at least ConvertCSharp to get its name will work).
            TypeReference dataSerializerType;
            var className = ComplexSerializerRegistry.SerializerTypeName(type, false, true);
            if (type.HasGenericParameters)
                className += "`" + type.GenericParameters.Count;
            if (isLocal && type is TypeDefinition)
            {
                dataSerializerType = new TypeDefinition("Stride.Core.DataSerializers", className,
                    TypeAttributes.AnsiClass | TypeAttributes.AutoClass | TypeAttributes.Sealed |
                    TypeAttributes.BeforeFieldInit |
                    (type.HasGenericParameters ? TypeAttributes.Public : TypeAttributes.NotPublic));

                // TODO: Only if not using Roslyn
                Assembly.MainModule.Types.Add((TypeDefinition)dataSerializerType);
            }
            else
            {
                dataSerializerType = new TypeReference("Stride.Core.DataSerializers", className, type.Module, type.Scope);
            }

            var mode = DataSerializerGenericMode.None;
            if (type.HasGenericParameters)
            {
                mode = DataSerializerGenericMode.GenericArguments;

                // Clone generic parameters
                foreach (var genericParameter in type.GenericParameters)
                {
                    var newGenericParameter = new GenericParameter(genericParameter.Name, dataSerializerType)
                    {
                        Attributes = genericParameter.Attributes
                    };

                    // Clone type constraints (others will be in Attributes)
                    foreach (var constraint in genericParameter.Constraints)
                        newGenericParameter.Constraints.Add(constraint);

                    dataSerializerType.GenericParameters.Add(newGenericParameter);
                }
            }

            if (dataSerializerType is TypeDefinition dataSerializerTypeDefinition)
            {
                // Setup base class
                var resolvedType = type.Resolve();
                var useClassDataSerializer = resolvedType.IsClass && !resolvedType.IsValueType && !resolvedType.IsAbstract && !resolvedType.IsInterface && resolvedType.GetEmptyConstructor() != null;
                var classDataSerializerType = Assembly.GetStrideCoreModule().GetType(useClassDataSerializer ? "Stride.Core.Serialization.ClassDataSerializer`1" : "Stride.Core.Serialization.DataSerializer`1");
                var parentType = Assembly.MainModule.ImportReference(classDataSerializerType).MakeGenericType(type.MakeGenericType(dataSerializerType.GenericParameters.ToArray<TypeReference>()));
                //parentType = ResolveGenericsVisitor.Process(serializerType, type.BaseType);
                dataSerializerTypeDefinition.BaseType = parentType;
            }

            var serializableTypeInfo = new SerializableTypeInfo(dataSerializerType, true, mode);
            serializableTypeInfo.Local = type.Resolve().Module.Assembly == Assembly;
            AddSerializableType(type, serializableTypeInfo);

            if (isLocal && type is TypeDefinition)
            {
                ComplexTypes.Add((TypeDefinition)type, serializableTypeInfo);
            }

            serializableTypeInfo.ComplexSerializer = true;

            return serializableTypeInfo;
        }

        public void AddSerializableType(TypeReference dataType, SerializableTypeInfo serializableTypeInfo, string profile = "Default")
        {
            SerializableTypeInfo currentValue;

            // Check if declaring type is generics
            var resolvedType = dataType.Resolve();
            if (resolvedType != null && resolvedType.DeclaringType != null && (resolvedType.HasGenericParameters || resolvedType.DeclaringType.HasGenericParameters))
                throw new NotSupportedException(String.Format("Serialization of nested types referencing parent's generic parameters is not currently supported. " +
                                                              "[Nested type={0} Parent={1}]", resolvedType.FullName, resolvedType.DeclaringType));

            var profileInfo = GetSerializableTypes(profile);

            if (profileInfo.TryGetSerializableTypeInfo(dataType, serializableTypeInfo.Mode != DataSerializerGenericMode.None, out currentValue))
            {
                // TODO: Doesn't work in some generic case
                if (//currentValue.SerializerType.ConvertCSharp() != serializableTypeInfo.SerializerType.ConvertCSharp() ||
                    currentValue.Mode != serializableTypeInfo.Mode)
                    throw new InvalidOperationException(string.Format("Incompatible serializer found for same type in different assemblies for {0}", dataType.ConvertCSharp()));
                return;
            }

            // Check that we don't simply try to add the same serializer than Default profile (optimized)
            SerializableTypeInfo defaultValue;
            if (profile != "Default" && SerializableTypes.TryGetSerializableTypeInfo(dataType, serializableTypeInfo.Mode != DataSerializerGenericMode.None, out defaultValue))
            {
                if (defaultValue.SerializerType.FullName == serializableTypeInfo.SerializerType.FullName)
                {
                    // Already added in default profile, early exit
                    return;
                }
            }

            profileInfo.AddSerializableTypeInfo(dataType, serializableTypeInfo);

            // Scan and add dependencies (stored in EnumerateGenericInstantiations() functions).
            if (serializableTypeInfo.Local && serializableTypeInfo.SerializerType != null)
            {
                var resolvedSerializerType = serializableTypeInfo.SerializerType.Resolve();
                if (resolvedSerializerType != null)
                {
                    var enumerateGenericInstantiationsMethod = resolvedSerializerType.Methods.FirstOrDefault(x => x.Name == "EnumerateGenericInstantiations");
                    if (enumerateGenericInstantiationsMethod != null)
                    {
                        // Detect all ldtoken (attributes would have been better, but unfortunately C# doesn't allow generics in attributes)
                        foreach (var inst in enumerateGenericInstantiationsMethod.Body.Instructions)
                        {
                            if (inst.OpCode.Code == Code.Ldtoken)
                            {
                                var type = (TypeReference)inst.Operand;

                                // Try to "close" generics type with serializer type as a context
                                var dependentType = ResolveGenericsVisitor.Process(serializableTypeInfo.SerializerType, type);
                                if (!dependentType.ContainsGenericParameter())
                                {
                                    // Import type so that it becomes local to the assembly
                                    // (otherwise SerializableTypeInfo.Local will be false and it won't be instantiated)
                                    var importedType = Assembly.MainModule.ImportReference(dependentType);
                                    if (GenerateSerializer(importedType) == null)
                                    {
                                        throw new InvalidOperationException(string.Format("Could not find serializer for generic dependent type {0} when processing {1}", dependentType, dataType));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private ProfileInfo GetSerializableTypes(string profile)
        {
            ProfileInfo profileInfo;
            if (!SerializableTypesProfiles.TryGetValue(profile, out profileInfo))
            {
                profileInfo = new ProfileInfo();
                SerializableTypesProfiles.Add(profile, profileInfo);
            }
            return profileInfo;
        }

        internal class SerializableTypeInfo
        {
            public TypeReference SerializerType { get; internal set; }
            public DataSerializerGenericMode Mode { get; internal set; }

            // True if type is created in current assembly
            public bool Local;

            // True if the serializer is defined manually by a hand-written DataSerializerGlobalAttribute in current assembly
            public bool ExistingLocal;

            /// <summary>
            /// True if serializer is inherited (i.e. DataSerializer(Inherited == true)).
            /// </summary>
            public bool Inherited;
            
            /// <summary>
            /// True if it's a complex serializer.
            /// </summary>
            public bool ComplexSerializer;

            /// <summary>
            /// True if it's a complex serializer and its base class should be serialized too.
            /// </summary>
            public bool ComplexSerializerProcessParentType;

            public SerializableTypeInfo(TypeReference serializerType, bool local, DataSerializerGenericMode mode = DataSerializerGenericMode.None)
            {
                SerializerType = serializerType;
                Mode = mode;
                Local = local;
            }
        }

        public class ProfileInfo
        {
            /// <summary>
            /// Serializable types (Mode is always None).
            /// </summary>
            public Dictionary<TypeReference, SerializableTypeInfo> SerializableTypes = new Dictionary<TypeReference, SerializableTypeInfo>(TypeReferenceEqualityComparer.Default);

            public bool IsFrozen { get; set; }

            /// <summary>
            /// Generic serializable types.
            /// </summary>
            public Dictionary<TypeReference, SerializableTypeInfo> GenericSerializableTypes = new Dictionary<TypeReference, SerializableTypeInfo>(TypeReferenceEqualityComparer.Default);

            public bool TryGetSerializableTypeInfo(TypeReference type, bool generic, out SerializableTypeInfo result)
            {
                return generic
                    ? GenericSerializableTypes.TryGetValue(type, out result)
                    : SerializableTypes.TryGetValue(type, out result);
            }

            public void AddSerializableTypeInfo(TypeReference typeReference, SerializableTypeInfo serializableTypeInfo)
            {
                if (serializableTypeInfo.Mode != DataSerializerGenericMode.None)
                    GenericSerializableTypes.Add(typeReference, serializableTypeInfo);
                else
                {
                    if (IsFrozen)
                    {
                        throw new InvalidOperationException(string.Format("Unexpected type [{0}] to add while serializable types are frozen", typeReference));
                    }
                    SerializableTypes.Add(typeReference, serializableTypeInfo);
                }
            }
        }
    }
}
