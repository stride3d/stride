// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using Stride.Core.Serialization;

namespace Stride.Core.AssemblyProcessor.Serializers;

internal class CecilSerializerContext
{
    private readonly TextWriter log;

    public CecilSerializerContext(PlatformType platform, AssemblyDefinition assembly, TextWriter log)
    {
        Platform = platform;
        Assembly = assembly;
        SerializableTypesProfiles = [];
        SerializableTypes = new ProfileInfo();
        SerializableTypesProfiles.Add("Default", SerializableTypes);
        ComplexTypes = [];
        this.log = log;

        StrideCoreModule = assembly.GetStrideCoreModule();
    }

    public PlatformType Platform { get; }

    /// <summary>
    /// Gets the assembly being processed.
    /// </summary>
    public AssemblyDefinition Assembly { get; }

    public ModuleDefinition StrideCoreModule { get; }

    public List<Tuple<string, TypeDefinition, bool>> DataContractAliases { get; } = [];

    /// <summary>
    /// Gets the list of serializable type grouped by profile.
    /// </summary>
    public Dictionary<string, ProfileInfo> SerializableTypesProfiles { get; }

    /// <summary>
    /// Gets the set of type that can be serialized (key) with their serializer name (string), corresponding to the "Default" profile.
    /// </summary>
    public ProfileInfo SerializableTypes { get; }

    /// <summary>
    /// Gets the list of complex serializers to generate.
    /// </summary>
    public Dictionary<TypeDefinition, SerializableTypeInfo> ComplexTypes { get; }

    /// <summary>
    /// Ensure the following type can be serialized. If not, try to register appropriate serializer.
    /// This method can be recursive.
    /// </summary>
    public SerializableTypeInfo GenerateSerializer(TypeReference type, bool force = true, string profile = "Default", bool generic = false)
    {
        var serializableTypes = GetSerializableTypes(profile);

        // Already handled?
        if (serializableTypes.TryGetSerializableTypeInfo(type, generic, out var serializableTypeInfo))
            return serializableTypeInfo;
        if (generic && serializableTypes.TryGetSerializableTypeInfo(type, false, out serializableTypeInfo))
            return serializableTypeInfo;

        // Arrays
        if (type is ArrayType arrayType)
            return GenerateArraySerializer(arrayType, force, profile);

        // Generic instances (List<T>, Dictionary<K,V>, etc.)
        if (type is GenericInstanceType genericInstanceType)
        {
            serializableTypeInfo = GenerateGenericSerializer(genericInstanceType, profile);
            if (serializableTypeInfo != null)
                return serializableTypeInfo;
        }

        // Check complex type definitions (only in Default profile)
        if (profile == "Default")
        {
            serializableTypeInfo = FindSerializerInfo(type, generic);
            if (serializableTypeInfo != null)
                return serializableTypeInfo;
        }

        // Non-Default profiles fall back to Default
        if (profile != "Default")
            return GenerateSerializer(type, force, "Default", generic);

        // Past this point, only proceed if a serializer is absolutely necessary.
        // This is skipped when scanning normal assembly types that might have nothing to do with serialization.
        if (!force)
            return null;

        // Non-instantiable types (object, interfaces, abstract classes)
        // Serializer can be null since they will be inherited anyway (handled through MemberSerializer)
        var resolvedType = type.Resolve();
        if (resolvedType.IsAbstract || resolvedType.IsInterface || resolvedType.FullName == typeof(object).FullName)
        {
            serializableTypeInfo = new SerializableTypeInfo(null, true);
            AddSerializableType(type, serializableTypeInfo, profile);
            return serializableTypeInfo;
        }

        return null;
    }

    private SerializableTypeInfo GenerateArraySerializer(ArrayType arrayType, bool force, string profile)
    {
        // Only proceed if element type is serializable
        if (GenerateSerializer(arrayType.ElementType, force, profile) == null)
            return null;

        // Non-Default profiles fall back to Default for array serializer registration
        if (profile != "Default")
            return GenerateSerializer(arrayType, force, "Default");

        var arraySerializerType = StrideCoreModule.GetTypeResolved("Stride.Core.Serialization.Serializers.ArraySerializer`1");
        var serializerType = new GenericInstanceType(arraySerializerType);
        serializerType.GenericArguments.Add(arrayType.ElementType);

        var info = new SerializableTypeInfo(serializerType, true);
        AddSerializableType(arrayType, info, profile);
        return info;
    }

    private SerializableTypeInfo GenerateGenericSerializer(GenericInstanceType type, string profile)
    {
        // Try to match with existing generic serializer (for List, Dictionary, etc.)
        var elementInfo = GenerateSerializer(type.ElementType, false, profile, true);
        if (elementInfo == null)
            return null;

        var serializerType = InstantiateSerializerType(elementInfo.SerializerType, elementInfo.Mode, type, type.GenericArguments);

        var info = new SerializableTypeInfo(serializerType, true) { ComplexSerializer = elementInfo.ComplexSerializer };
        AddSerializableType(type, info, profile);

        if (elementInfo.ComplexSerializer)
            ProcessComplexSerializerMembers(type, info);

        return info;
    }

    /// <summary>
    /// Constructs a concrete serializer type from an open generic serializer definition
    /// by adding type arguments according to the <see cref="DataSerializerGenericMode"/>.
    /// </summary>
    private static GenericInstanceType InstantiateSerializerType(
        TypeReference openSerializerType,
        DataSerializerGenericMode mode,
        TypeReference dataType,
        IEnumerable<TypeReference> genericArguments)
    {
        var serializerType = new GenericInstanceType(openSerializerType);

        // Add the data type itself as first arg for Type/TypeAndGenericArguments modes
        if (mode is DataSerializerGenericMode.Type or DataSerializerGenericMode.TypeAndGenericArguments)
            serializerType.GenericArguments.Add(dataType);

        // Add the data type's generic arguments for GenericArguments/TypeAndGenericArguments modes
        if (mode is DataSerializerGenericMode.GenericArguments or DataSerializerGenericMode.TypeAndGenericArguments)
        {
            foreach (var arg in genericArguments)
                serializerType.GenericArguments.Add(arg);
        }

        return serializerType;
    }

    private void ProcessComplexSerializerMembers(TypeReference type, SerializableTypeInfo serializableTypeInfo, string profile = "Default")
    {
        // Process base type (for complex serializers)
        // Check if we have any serializable closed base type and collect it to pass it over to GenerateSerializerCode later,
        // who'll ensure the generated serializer for this type calls into its base types' serializer
        for (var baseType = type; (baseType = ResolveGenericsVisitor.Process(baseType, baseType.Resolve().BaseType)) != null;)
        {
            if (baseType.ContainsGenericParameter())
                continue; // ResolveGenericsVisitor failed, the type it returned is not closed, we can't serialize it

            var parentSerializableTypeInfo = GenerateSerializer(baseType, false, profile);
            if (parentSerializableTypeInfo?.SerializerType != null)
            {
                serializableTypeInfo.ComplexSerializerProcessParentType = baseType;
                break;
            }
        }

        // Process members
        foreach (var serializableItem in ComplexSerializerRegistry.GetSerializableItems(type, true))
        {
            // Check that all closed types have a proper serializer
            if (serializableItem.Attributes.Any(x => x.AttributeType.FullName == "Stride.Core.DataMemberCustomSerializerAttribute")
                || serializableItem.Type.ContainsGenericParameter())
            {
                continue;
            }

            var resolvedType = serializableItem.Type.Resolve();
            var isInterface = resolvedType?.IsInterface == true;

            try
            {
                if (GenerateSerializer(serializableItem.Type, profile: profile) == null)
                {
                    ComplexSerializerRegistry.IgnoreMember(serializableItem.MemberInfo);
                    if (!isInterface)
                    {
                        log.Write(
                            $"Warning: Member {serializableItem.MemberInfo} does not have a valid serializer. Add [DataMemberIgnore], turn the member non-public, or add a [DataContract] to it's type.");
                    }
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Could not process serialization for member {serializableItem.MemberInfo}", e);
            }
        }
    }

    /// <summary>
    /// Finds the serializer information by inspecting the type's attributes and inheritance chain.
    /// </summary>
    internal SerializableTypeInfo FindSerializerInfo(TypeReference type, bool generic)
    {
        if (type == null || type.FullName == typeof(object).FullName || type.FullName == typeof(ValueType).FullName || type.IsGenericParameter)
            return null;

        var resolvedType = type.Resolve();

        // Nested type — must be publicly accessible
        if (resolvedType.IsNested && !resolvedType.IsNestedPublic && !resolvedType.IsNestedAssembly)
            return null;

        // Enums
        if (resolvedType.IsEnum)
        {
            var enumSerializerType = StrideCoreModule.GetTypeResolved("Stride.Core.Serialization.Serializers.EnumSerializer`1");
            var serializerType = new GenericInstanceType(enumSerializerType);
            serializerType.GenericArguments.Add(type);

            var info = new SerializableTypeInfo(serializerType, true, DataSerializerGenericMode.None);
            AddSerializableType(type, info);
            return info;
        }

        // [DataSerializer] attribute — explicit serializer assignment
        var dataSerializerAttribute = resolvedType.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "Stride.Core.Serialization.DataSerializerAttribute");
        if (dataSerializerAttribute != null)
            return ProcessDataSerializerAttribute(type, dataSerializerAttribute, generic);

        // [DataContract] attribute — auto-generate complex serializer
        var dataContractAttribute = resolvedType.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "Stride.Core.DataContractAttribute");
        if (dataContractAttribute != null)
        {
            var inherited = dataContractAttribute.Properties
                .Where(x => x.Name == "Inherited")
                .Select(x => (bool)x.Argument.Value)
                .FirstOrDefault();

            var info = CreateComplexSerializer(type);
            info.Inherited = inherited;
            ProcessComplexSerializerMembers(type, info);
            return info;
        }

        // Check if parent type has Inherited attribute
        return FindInheritedSerializerInfo(type, generic);
    }

    private SerializableTypeInfo ProcessDataSerializerAttribute(TypeReference type, CustomAttribute attribute, bool generic)
    {
        var modeField = attribute.Fields.FirstOrDefault(x => x.Name == "Mode");
        var mode = (modeField.Name != null) ? (DataSerializerGenericMode)modeField.Argument.Value : DataSerializerGenericMode.None;
        var dataSerializerType = ((TypeReference)attribute.ConstructorArguments[0].Value).FixupValueType();

        if (mode is not (DataSerializerGenericMode.Type or DataSerializerGenericMode.TypeAndGenericArguments)
            || (mode == DataSerializerGenericMode.TypeAndGenericArguments && type is not GenericInstanceType))
        {
            // Simple non-generic or GenericArguments mode
            var info = new SerializableTypeInfo(dataSerializerType, true, mode) { Inherited = false };
            AddSerializableType(type, info);
            return info;
        }

        // Type or TypeAndGenericArguments mode — register both generic and concrete versions
        var genericInfo = new SerializableTypeInfo(dataSerializerType, true, mode) { Inherited = true };
        AddSerializableType(type, genericInfo);

        var genericArguments = type is GenericInstanceType git ? git.GenericArguments : [];
        var actualSerializerType = InstantiateSerializerType(dataSerializerType, mode, type, genericArguments);

        var concreteInfo = new SerializableTypeInfo(actualSerializerType, true);
        AddSerializableType(type, concreteInfo);

        return generic ? genericInfo : concreteInfo;
    }

    private SerializableTypeInfo FindInheritedSerializerInfo(TypeReference type, bool generic)
    {
        var parentType = ResolveGenericsVisitor.Process(type, type.Resolve().BaseType);
        if (parentType == null)
            return null;

        // Generate serializer for parent type
        var parentInfo = GenerateSerializer(parentType.Resolve(), false, generic: true);
        if (parentInfo?.Inherited != true)
            return null;

        // Parent has a complex serializer — create one for this type too
        if (parentInfo.ComplexSerializer)
        {
            var info = CreateComplexSerializer(type);
            info.Inherited = true;
            ProcessComplexSerializerMembers(type, info);
            return info;
        }

        // Parent has a Type/TypeAndGenericArguments mode serializer — inherit it
        if (parentInfo.Mode is DataSerializerGenericMode.Type or DataSerializerGenericMode.TypeAndGenericArguments)
        {
            // Register generic version
            var genericInfo = new SerializableTypeInfo(parentInfo.SerializerType, true, parentInfo.Mode);
            AddSerializableType(type, genericInfo);

            if (!type.HasGenericParameters)
            {
                var genericArguments = parentType is GenericInstanceType git ? git.GenericArguments : [];
                var actualSerializerType = InstantiateSerializerType(parentInfo.SerializerType, parentInfo.Mode, type, genericArguments);

                var concreteInfo = new SerializableTypeInfo(actualSerializerType, true);
                AddSerializableType(type, concreteInfo);

                if (!generic)
                    return concreteInfo;
            }

            return genericInfo;
        }

        throw new InvalidOperationException("Not sure how to process this inherited serializer");
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
            dataSerializerTypeDefinition.BaseType = parentType;
        }

        var serializableTypeInfo = new SerializableTypeInfo(dataSerializerType, true, mode)
        {
            Local = type.Resolve().Module.Assembly == Assembly
        };
        AddSerializableType(type, serializableTypeInfo);

        if (isLocal && type is TypeDefinition definition)
        {
            ComplexTypes.Add(definition, serializableTypeInfo);
        }

        serializableTypeInfo.ComplexSerializer = true;

        return serializableTypeInfo;
    }

    public void AddSerializableType(TypeReference dataType, SerializableTypeInfo serializableTypeInfo, string profile = "Default")
    {
        // Check if declaring type is generics
        var resolvedType = dataType.Resolve();
        if (resolvedType?.DeclaringType != null && (resolvedType.HasGenericParameters || resolvedType.DeclaringType.HasGenericParameters))
        {
            throw new NotSupportedException(string.Format("Serialization of nested types referencing parent's generic parameters is not currently supported. " +
                                                          "[Nested type={0} Parent={1}]", resolvedType.FullName, resolvedType.DeclaringType));
        }

        var profileInfo = GetSerializableTypes(profile);

        if (profileInfo.TryGetSerializableTypeInfo(dataType, serializableTypeInfo.Mode != DataSerializerGenericMode.None, out var currentValue))
        {
            // TODO: Doesn't work in some generic case
            if (currentValue.Mode != serializableTypeInfo.Mode)
                throw new InvalidOperationException(string.Format("Incompatible serializer found for same type in different assemblies for {0}", dataType.ConvertCSharp()));
            return;
        }

        // Check that we don't simply try to add the same serializer than Default profile (optimized)
        if (profile != "Default" && SerializableTypes.TryGetSerializableTypeInfo(dataType, serializableTypeInfo.Mode != DataSerializerGenericMode.None, out var defaultValue))
        {
            if (defaultValue.SerializerType.FullName == serializableTypeInfo.SerializerType.FullName)
            {
                // Already added in default profile, early exit
                return;
            }
        }

        profileInfo.AddSerializableTypeInfo(dataType, serializableTypeInfo);

        // Scan and add dependencies (stored in EnumerateGenericInstantiations() methods)
        ScanSerializerDependencies(dataType, serializableTypeInfo);
    }

    /// <summary>
    /// Scans a serializer type's EnumerateGenericInstantiations method for ldtoken instructions
    /// to discover dependent types that also need serializers.
    /// </summary>
    private void ScanSerializerDependencies(TypeReference dataType, SerializableTypeInfo serializableTypeInfo)
    {
        if (!serializableTypeInfo.Local || serializableTypeInfo.SerializerType == null)
            return;

        var resolvedSerializerType = serializableTypeInfo.SerializerType.Resolve();
        if (resolvedSerializerType == null)
            return;

        var enumerateMethod = resolvedSerializerType.Methods.FirstOrDefault(x => x.Name == "EnumerateGenericInstantiations");
        if (enumerateMethod == null)
            return;

        // Detect all ldtoken (attributes would have been better, but unfortunately C# doesn't allow generics in attributes)
        foreach (var inst in enumerateMethod.Body.Instructions)
        {
            if (inst.OpCode.Code != Code.Ldtoken)
                continue;

            var type = (TypeReference)inst.Operand;

            // Try to "close" generics type with serializer type as a context
            var dependentType = ResolveGenericsVisitor.Process(serializableTypeInfo.SerializerType, type);
            if (dependentType.ContainsGenericParameter())
                continue;

            // Import type so that it becomes local to the assembly
            // (otherwise SerializableTypeInfo.Local will be false and it won't be instantiated)
            var importedType = Assembly.MainModule.ImportReference(dependentType);
            if (GenerateSerializer(importedType) == null)
            {
                throw new InvalidOperationException(string.Format("Could not find serializer for generic dependent type {0} when processing {1}", dependentType, dataType));
            }
        }
    }

    private ProfileInfo GetSerializableTypes(string profile)
    {
        if (!SerializableTypesProfiles.TryGetValue(profile, out var profileInfo))
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
        /// Not null if it's a complex serializer and its base class should be serialized too.
        /// </summary>
        public TypeReference? ComplexSerializerProcessParentType;

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
        public Dictionary<TypeReference, SerializableTypeInfo> SerializableTypes = new(TypeReferenceEqualityComparer.Default);

        public bool IsFrozen { get; set; }

        /// <summary>
        /// Generic serializable types.
        /// </summary>
        public Dictionary<TypeReference, SerializableTypeInfo> GenericSerializableTypes = new(TypeReferenceEqualityComparer.Default);

        public bool TryGetSerializableTypeInfo(TypeReference type, bool generic, out SerializableTypeInfo result)
        {
            return generic
                ? GenericSerializableTypes.TryGetValue(type, out result)
                : SerializableTypes.TryGetValue(type, out result);
        }

        public void AddSerializableTypeInfo(TypeReference typeReference, SerializableTypeInfo serializableTypeInfo)
        {
            if (serializableTypeInfo.Mode != DataSerializerGenericMode.None)
            {
                GenericSerializableTypes.Add(typeReference, serializableTypeInfo);
            }
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
