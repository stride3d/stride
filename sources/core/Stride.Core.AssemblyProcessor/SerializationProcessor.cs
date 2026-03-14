// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Stride.Core.AssemblyProcessor.Serializers;
using Stride.Core.Serialization;
using Stride.Core.Storage;
using CustomAttributeNamedArgument = Mono.Cecil.CustomAttributeNamedArgument;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Stride.Core.AssemblyProcessor;

internal class SerializationProcessor : IAssemblyDefinitionProcessor
{
    public bool Process(AssemblyProcessorContext context)
    {
        var registry = new ComplexSerializerRegistry(context.Platform, context.Assembly, context.Log);

        // Register default serialization profile (to help AOT generic instantiation of serializers)
        RegisterDefaultSerializationProfile(context.AssemblyResolver, context.Assembly, registry, context.Log);

        // Generate serializer code using Cecil and ILBuilder
        GenerateSerializerCode(registry, out var serializationHash);

        context.SerializationHash = serializationHash;

        return true;
    }

    /// <summary>
    /// Generates serializer code using Cecil and <see cref="ILBuilder"/> for readable IL emission.
    /// </summary>
    private static void GenerateSerializerCode(ComplexSerializerRegistry registry, out ObjectId serializationHash)
    {
        var hash = new ObjectIdBuilder();

        // First, hash global binary format version, in case it gets bumped
        hash.Write(DataSerializer.BinaryFormatVersion);

        var assembly = registry.Assembly;
        var module = assembly.MainModule;
        var strideCoreModule = assembly.GetStrideCoreModule();

        // Generate serializer classes for each complex type
        GenerateComplexSerializerTypes(registry, module, strideCoreModule, hash);

        // Generate the factory type with attributes and module initializer
        GenerateSerializerFactory(registry, assembly, module, strideCoreModule);

        serializationHash = hash.ComputeHash();
    }

    /// <summary>
    /// Generates serializer classes (constructor, Initialize, Serialize methods) for each complex type.
    /// </summary>
    private static void GenerateComplexSerializerTypes(
        ComplexSerializerRegistry registry,
        ModuleDefinition module,
        ModuleDefinition strideCoreModule,
        ObjectIdBuilder hash)
    {
        var dataSerializerTypeRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.DataSerializer`1"));
        var serializerSelectorType = strideCoreModule.GetType("Stride.Core.Serialization.SerializerSelector");
        var serializerSelectorTypeRef = module.ImportReference(serializerSelectorType);
        var serializerSelectorGetSerializerRef = module.ImportReference(serializerSelectorType.Methods.Single(x => x.Name == "GetSerializer" && x.Parameters.Count == 0 && x.GenericParameters.Count == 1));
        var memberSerializerCreateRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.MemberSerializer`1").Methods.Single(x => x.Name == "Create"));

        var dataSerializerSerializeMethod = dataSerializerTypeRef.Resolve().Methods.Single(x => x.Name == "Serialize" && (x.Attributes & MethodAttributes.Abstract) != 0);
        var dataSerializerSerializeMethodRef = module.ImportReference(dataSerializerSerializeMethod);

        foreach (var complexType in registry.Context.ComplexTypes)
        {
            var type = complexType.Key;
            var serializerType = (TypeDefinition)complexType.Value.SerializerType;
            var genericParameters = serializerType.GenericParameters.ToArray<TypeReference>();
            var typeWithGenerics = type.MakeGenericType(genericParameters);

            // Hash
            hash.Write(typeWithGenerics.FullName);

            TypeReference parentType = null;
            FieldDefinition parentSerializerField = null;
            if (complexType.Value.ComplexSerializerProcessParentType != null)
            {
                parentType = complexType.Value.ComplexSerializerProcessParentType;
                serializerType.Fields.Add(parentSerializerField = new FieldDefinition("parentSerializer", Mono.Cecil.FieldAttributes.Private, dataSerializerTypeRef.MakeGenericType(parentType)));

                hash.Write("parent");
            }

            var serializableItems = ComplexSerializerRegistry.GetSerializableItems(type, true).ToArray();
            var serializableItemInfos = new Dictionary<TypeReference, (FieldDefinition SerializerField, TypeReference Type)>(TypeReferenceEqualityComparer.Default);
            var localsByTypes = new Dictionary<TypeReference, VariableDefinition>(TypeReferenceEqualityComparer.Default);

            ResolveGenericsVisitor genericResolver = null;
            if (type.HasGenericParameters)
            {
                var genericMapping = new Dictionary<TypeReference, TypeReference>();
                for (int i = 0; i < type.GenericParameters.Count; i++)
                {
                    genericMapping[type.GenericParameters[i]] = serializerType.GenericParameters[i];
                }
                genericResolver = new ResolveGenericsVisitor(genericMapping);
            }

            foreach (var serializableItem in serializableItems)
            {
                if (serializableItemInfos.ContainsKey(serializableItem.Type))
                    continue;

                var serializableItemType = serializableItem.Type;
                if (genericResolver != null)
                    serializableItemType = genericResolver.VisitDynamic(serializableItemType);
                var fieldDefinition = new FieldDefinition($"{Utilities.BuildValidClassName(serializableItemType.FullName)}Serializer", Mono.Cecil.FieldAttributes.Private, dataSerializerTypeRef.MakeGenericType(serializableItemType));
                serializableItemInfos.Add(serializableItem.Type, (fieldDefinition, serializableItemType));
                serializerType.Fields.Add(fieldDefinition);

                hash.Write(serializableItem.Type.FullName);
                hash.Write(serializableItem.Name);
                hash.Write(serializableItem.AssignBack);
            }

            // Add constructor (call parent constructor)
            var ctor = new MethodDefinition(".ctor",
                MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig |
                MethodAttributes.Public, module.TypeSystem.Void);
            var ctorIL = new ILBuilder(ctor.Body, module);
            ctorIL.Emit(OpCodes.Ldarg_0)
                  .Emit(OpCodes.Call, ctorIL.Import(serializerType.BaseType.Resolve().GetEmptyConstructor(true)).MakeGeneric(typeWithGenerics))
                  .Emit(OpCodes.Ret);
            serializerType.Methods.Add(ctor);

            // Add Initialize method
            var initialize = new MethodDefinition("Initialize", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, module.TypeSystem.Void);
            initialize.Parameters.Add(new ParameterDefinition("serializerSelector", ParameterAttributes.None, serializerSelectorTypeRef));
            var initIL = new ILBuilder(initialize.Body, module);
            if (complexType.Value.ComplexSerializerProcessParentType != null)
            {
                initIL.Emit(OpCodes.Ldarg_0)
                      .Emit(OpCodes.Ldarg_1)
                      .Emit(OpCodes.Callvirt, serializerSelectorGetSerializerRef.MakeGenericMethod(parentType))
                      .Emit(OpCodes.Stfld, parentSerializerField.MakeGeneric(genericParameters));
            }
            foreach (var serializableItem in serializableItemInfos)
            {
                initIL.Emit(OpCodes.Ldarg_0)
                      .Emit(OpCodes.Ldarg_1)
                      .Emit(OpCodes.Ldc_I4_1)
                      .Emit(OpCodes.Call, memberSerializerCreateRef.MakeGeneric(serializableItem.Value.Type))
                      .Emit(OpCodes.Stfld, serializableItem.Value.SerializerField.MakeGeneric(genericParameters));
            }
            initIL.Emit(OpCodes.Ret);
            serializerType.Methods.Add(initialize);

            // Add Serialize method
            GenerateSerializeMethod(type, serializerType, genericParameters, typeWithGenerics,
                complexType.Value, serializableItems, serializableItemInfos, localsByTypes,
                dataSerializerSerializeMethod, dataSerializerSerializeMethodRef,
                parentType, parentSerializerField, module);
        }
    }

    /// <summary>
    /// Generates the Serialize method for a complex serializer type.
    /// </summary>
    private static void GenerateSerializeMethod(
        TypeDefinition type,
        TypeDefinition serializerType,
        TypeReference[] genericParameters,
        TypeReference typeWithGenerics,
        CecilSerializerContext.SerializableTypeInfo typeInfo,
        ComplexSerializerRegistry.SerializableItem[] serializableItems,
        Dictionary<TypeReference, (FieldDefinition SerializerField, TypeReference Type)> serializableItemInfos,
        Dictionary<TypeReference, VariableDefinition> localsByTypes,
        MethodDefinition dataSerializerSerializeMethod,
        MethodReference dataSerializerSerializeMethodRef,
        TypeReference parentType,
        FieldDefinition parentSerializerField,
        ModuleDefinition module)
    {
        var serialize = new MethodDefinition("Serialize", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, module.TypeSystem.Void);
        serialize.Parameters.Add(new ParameterDefinition("obj", ParameterAttributes.None, typeWithGenerics.MakeByReferenceType()));
        // Copy other parameters from parent method
        for (int i = 1; i < dataSerializerSerializeMethod.Parameters.Count; ++i)
        {
            var parentParameter = dataSerializerSerializeMethod.Parameters[i];
            serialize.Parameters.Add(new ParameterDefinition(parentParameter.Name, ParameterAttributes.None, module.ImportReference(parentParameter.ParameterType)));
        }

        var il = new ILBuilder(serialize.Body, module);

        if (typeInfo.ComplexSerializerProcessParentType != null)
        {
            il.Emit(OpCodes.Ldarg_0)
              .Emit(OpCodes.Ldfld, parentSerializerField.MakeGeneric(genericParameters))
              .Emit(OpCodes.Ldarg_1)
              .Emit(OpCodes.Ldarg_2)
              .Emit(OpCodes.Ldarg_3)
              .Emit(OpCodes.Callvirt, dataSerializerSerializeMethodRef.MakeGeneric(parentType));
        }

        if (serializableItems.Length > 0)
        {
            var deserializeLabel = ILBuilder.DefineLabel();
            var endLabel = ILBuilder.DefineLabel();

            // Iterate over ArchiveMode
            for (int i = 0; i < 2; ++i)
            {
                var archiveMode = i == 0 ? ArchiveMode.Serialize : ArchiveMode.Deserialize;

                // Check mode
                if (archiveMode == ArchiveMode.Serialize)
                {
                    il.Emit(OpCodes.Ldarg_2)
                      .Emit(OpCodes.Ldc_I4, (int)archiveMode)
                      .Emit(OpCodes.Ceq)
                      .Emit(OpCodes.Brfalse, deserializeLabel);
                }
                else
                {
                    il.MarkLabel(deserializeLabel);
                }

                foreach (var serializableItem in serializableItems)
                {
                    if (serializableItem.HasFixedAttribute)
                    {
                        throw new NotImplementedException("FixedBuffer attribute is not supported.");
                    }

                    var memberAssignBack = serializableItem.AssignBack;
                    var memberVariableName = (serializableItem.MemberInfo is PropertyDefinition || !memberAssignBack) ? ComplexSerializerRegistry.CreateMemberVariableName(serializableItem.MemberInfo) : null;
                    var serializableItemInfo = serializableItemInfos[serializableItem.Type];
                    il.Emit(OpCodes.Ldarg_0)
                      .Emit(OpCodes.Ldfld, serializableItemInfo.SerializerField.MakeGeneric(genericParameters));

                    var fieldReference = serializableItem.MemberInfo is FieldReference ? il.Import((FieldReference)serializableItem.MemberInfo).MakeGeneric(genericParameters) : null;

                    if (memberVariableName != null)
                    {
                        // Use a temporary variable
                        if (!localsByTypes.TryGetValue(serializableItemInfo.Type, out var tempLocal))
                        {
                            tempLocal = il.AddLocal(serializableItemInfo.Type);
                            localsByTypes.Add(serializableItemInfo.Type, tempLocal);
                        }

                        if (!(archiveMode == ArchiveMode.Deserialize && memberAssignBack))
                        {
                            // obj.Member
                            il.Emit(OpCodes.Ldarg_1);
                            if (!type.IsValueType)
                                il.Emit(OpCodes.Ldind_Ref);

                            if (serializableItem.MemberInfo is PropertyDefinition property)
                            {
                                var getMethod = property.Resolve().GetMethod;
                                il.Emit(getMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, il.Import(getMethod).MakeGeneric(genericParameters));
                            }
                            else if (serializableItem.MemberInfo is FieldDefinition)
                            {
                                il.Emit(OpCodes.Ldfld, fieldReference);
                            }
                            il.Emit(OpCodes.Stloc, tempLocal)
                              .Emit(OpCodes.Ldloca, tempLocal);
                        }
                        else
                        {
                            // default(T)
                            il.Emit(OpCodes.Ldloca, tempLocal)
                              .Emit(OpCodes.Dup)
                              .Emit(OpCodes.Initobj, serializableItemInfo.Type);
                        }
                    }
                    else
                    {
                        // Use object directly
                        il.Emit(OpCodes.Ldarg_1);
                        if (!type.IsValueType)
                            il.Emit(OpCodes.Ldind_Ref);
                        il.Emit(OpCodes.Ldflda, fieldReference);
                    }
                    il.Emit(OpCodes.Ldarg_2)
                      .Emit(OpCodes.Ldarg_3)
                      .Emit(OpCodes.Callvirt, dataSerializerSerializeMethodRef.MakeGeneric(serializableItemInfo.Type));

                    if (archiveMode == ArchiveMode.Deserialize && memberVariableName != null && memberAssignBack)
                    {
                        // Need to copy back to object
                        il.Emit(OpCodes.Ldarg_1);
                        if (!type.IsValueType)
                            il.Emit(OpCodes.Ldind_Ref);

                        il.Emit(OpCodes.Ldloc, localsByTypes[serializableItemInfo.Type]);

                        if (serializableItem.MemberInfo is PropertyDefinition property)
                        {
                            var setMethod = property.Resolve().SetMethod;
                            il.Emit(setMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, il.Import(setMethod).MakeGeneric(genericParameters));
                        }
                        else if (serializableItem.MemberInfo is FieldDefinition)
                        {
                            il.Emit(OpCodes.Stfld, fieldReference);
                        }
                    }
                }

                if (archiveMode == ArchiveMode.Serialize)
                {
                    il.Emit(OpCodes.Br, endLabel);
                }
            }

            il.MarkLabel(endLabel);
        }
        il.Emit(OpCodes.Ret);
        serializerType.Methods.Add(serialize);
    }

    /// <summary>
    /// Generates the serializer factory type with <see cref="DataSerializerGlobalAttribute"/>s
    /// and the module initializer that registers all serializers at runtime.
    /// </summary>
    private static void GenerateSerializerFactory(
        ComplexSerializerRegistry registry,
        AssemblyDefinition assembly,
        ModuleDefinition module,
        ModuleDefinition strideCoreModule)
    {
        var typeTypeRef = module.ImportReference(CecilExtensions.FindCorlibAssembly(assembly).MainModule.GetTypeResolved(typeof(Type).FullName));

        // Create factory type
        var serializerFactoryType = new TypeDefinition("Stride.Core.DataSerializers",
            Utilities.BuildValidClassName(assembly.Name.Name) + "SerializerFactory",
            TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract,
            module.TypeSystem.Object);
        module.Types.Add(serializerFactoryType);

        // Add [DataSerializerGlobal] attributes for each serializable type
        EmitDataSerializerGlobalAttributes(registry, module, strideCoreModule, typeTypeRef, serializerFactoryType);

        // Generate the Initialize method (module initializer body)
        GenerateInitializeMethod(registry, assembly, module, strideCoreModule, serializerFactoryType);

        // Add [AssemblySerializerFactory] attribute to the assembly
        var assemblySerializerFactoryAttribute = strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializerFactoryAttribute");
        assembly.CustomAttributes.Add(new CustomAttribute(module.ImportReference(assemblySerializerFactoryAttribute.GetEmptyConstructor()))
        {
            Fields =
            {
                new CustomAttributeNamedArgument("Type", new CustomAttributeArgument(typeTypeRef, serializerFactoryType)),
            }
        });
    }

    /// <summary>
    /// Adds [DataSerializerGlobal] attributes to the factory type for each serializable type.
    /// </summary>
    private static void EmitDataSerializerGlobalAttributes(
        ComplexSerializerRegistry registry,
        ModuleDefinition module,
        ModuleDefinition strideCoreModule,
        TypeReference typeTypeRef,
        TypeDefinition serializerFactoryType)
    {
        var dataSerializerModeTypeRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.DataSerializerGenericMode"));
        var dataSerializerGlobalAttribute = strideCoreModule.GetType("Stride.Core.Serialization.DataSerializerGlobalAttribute");
        var dataSerializerGlobalCtorRef = module.ImportReference(dataSerializerGlobalAttribute.GetConstructors().Single(x => !x.IsStatic && x.Parameters.Count == 5));

        foreach (var profile in registry.Context.SerializableTypesProfiles)
        {
            // Emit attributes for both concrete and generic serializable types
            var allTypes = profile.Value.SerializableTypes.Where(x => x.Value.Local)
                .Concat(profile.Value.GenericSerializableTypes.Where(x => x.Value.Local));

            foreach (var type in allTypes)
            {
                serializerFactoryType.CustomAttributes.Add(new CustomAttribute(dataSerializerGlobalCtorRef)
                {
                    ConstructorArguments =
                    {
                        new CustomAttributeArgument(typeTypeRef, type.Value.SerializerType != null ? module.ImportReference(type.Value.SerializerType) : null),
                        new CustomAttributeArgument(typeTypeRef, module.ImportReference(type.Key)),
                        new CustomAttributeArgument(dataSerializerModeTypeRef, type.Value.Mode),
                        new CustomAttributeArgument(module.TypeSystem.Boolean, type.Value.Inherited),
                        new CustomAttributeArgument(module.TypeSystem.Boolean, type.Value.ComplexSerializer),
                    },
                    Properties =
                    {
                        new CustomAttributeNamedArgument("Profile", new CustomAttributeArgument(module.TypeSystem.String, profile.Key))
                    },
                });
            }
        }
    }

    /// <summary>
    /// Generates the Initialize method that serves as the module initializer,
    /// registering all serializers with the runtime <see cref="DataSerializerFactory"/>.
    /// </summary>
    private static void GenerateInitializeMethod(
        ComplexSerializerRegistry registry,
        AssemblyDefinition assembly,
        ModuleDefinition module,
        ModuleDefinition strideCoreModule,
        TypeDefinition serializerFactoryType)
    {
        var mscorlibAssembly = CecilExtensions.FindCorlibAssembly(assembly);
        var reflectionAssembly = CecilExtensions.FindReflectionAssembly(assembly);

        // Create Initialize method
        var initializeMethod = new MethodDefinition("Initialize",
            MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Static,
            module.TypeSystem.Void);
        serializerFactoryType.Methods.Add(initializeMethod);

        var il = new ILBuilder(initializeMethod.Body, module);

        // Resolve and import reflection helpers
        var typeType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(Type).FullName);
        var getTypeFromHandleRef = il.Import(typeType.Methods.First(x => x.Name == nameof(Type.GetTypeFromHandle)));
        var getTypeInfoRef = il.Import(reflectionAssembly.MainModule.GetTypeResolved("System.Reflection.IntrospectionExtensions").Resolve().Methods.First(x => x.Name == nameof(IntrospectionExtensions.GetTypeInfo)));
        var typeInfoType = reflectionAssembly.MainModule.GetTypeResolved(typeof(TypeInfo).FullName);
        // Note: TypeInfo.Assembly/Module could be on the type itself or on its parent MemberInfo depending on runtime
        var typeInfoProperties = typeInfoType.Properties.Concat(typeInfoType.BaseType.Resolve().Properties);
        var getAssemblyRef = il.Import(typeInfoProperties.First(x => x.Name == nameof(TypeInfo.Assembly)).GetMethod);
        var getModuleRef = il.Import(typeInfoProperties.First(x => x.Name == nameof(TypeInfo.Module)).GetMethod);
        var getTypeHandleMethodRef = module.ImportReference(typeType.Properties.First(x => x.Name == nameof(Type.TypeHandle)).GetMethod);

        // var assemblySerializers = new AssemblySerializers(typeof(Factory).GetTypeInfo().Assembly);
        var assemblySerializersType = strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializers");
        il.EmitTypeofAssembly(serializerFactoryType, getTypeFromHandleRef, getTypeInfoRef, getAssemblyRef)
          .Emit(OpCodes.Newobj, il.Import(assemblySerializersType.Methods.Single(x => x.IsConstructor && x.Parameters.Count == 1)));

        // assemblySerializers.DataContractAliases.Add(...)
        var assemblySerializersGetDataContractAliasesRef = module.ImportReference(assemblySerializersType.Properties.First(x => x.Name == "DataContractAliases").GetMethod);
        var assemblySerializersGetDataContractAliasesAdd = assemblySerializersGetDataContractAliasesRef.ReturnType.Resolve().Methods.First(x => x.Name == "Add");
        var dataContractAliasTypeRef = ((GenericInstanceType)assemblySerializersGetDataContractAliasesRef.ReturnType).GenericArguments[0];
        var dataContractAliasTypeCtorRef = module.ImportReference(dataContractAliasTypeRef.Resolve().GetConstructors().Single());
        var assemblySerializersGetDataContractAliasesAddRef = module.ImportReference(assemblySerializersGetDataContractAliasesAdd).MakeGeneric(dataContractAliasTypeRef);

        foreach (var alias in registry.Context.DataContractAliases)
        {
            il.Emit(OpCodes.Dup)
              .Emit(OpCodes.Call, assemblySerializersGetDataContractAliasesRef)
              .Emit(OpCodes.Ldstr, alias.Item1)
              .EmitTypeof(il.Import(alias.Item2), getTypeFromHandleRef)
              .Emit(alias.Item3 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0)
              .Emit(OpCodes.Newobj, dataContractAliasTypeCtorRef)
              .Emit(OpCodes.Call, assemblySerializersGetDataContractAliasesAddRef);
        }

        // assemblySerializers.Modules.Add(typeof(ReferencedFactory).GetTypeInfo().Module)
        var assemblySerializersGetModulesRef = module.ImportReference(assemblySerializersType.Properties.First(x => x.Name == "Modules").GetMethod);
        var assemblySerializersGetModulesAdd = assemblySerializersGetModulesRef.ReturnType.Resolve().Methods.First(x => x.Name == "Add");
        var moduleRef = ((GenericInstanceType)assemblySerializersGetModulesRef.ReturnType).GenericArguments[0];
        var assemblySerializersGetModulesAddRef = module.ImportReference(assemblySerializersGetModulesAdd).MakeGeneric(moduleRef);

        foreach (var referencedAssemblySerializerFactoryType in registry.ReferencedAssemblySerializerFactoryTypes)
        {
            il.Emit(OpCodes.Dup)
              .Emit(OpCodes.Call, assemblySerializersGetModulesRef)
              .EmitTypeofModule(il.Import(referencedAssemblySerializerFactoryType), getTypeFromHandleRef, getTypeInfoRef, getModuleRef)
              .Emit(OpCodes.Call, assemblySerializersGetModulesAddRef);
        }

        // Per-profile serializer entry registration
        var objectIdCtorRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Storage.ObjectId").GetConstructors().Single(x => x.Parameters.Count == 4));
        var serializerEntryTypeCtorRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializerEntry").GetConstructors().Single());
        var assemblySerializersPerProfileType = strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializersPerProfile");
        var assemblySerializersPerProfileTypeAddRef = module.ImportReference(assemblySerializersPerProfileType.BaseType.Resolve().Methods.First(x => x.Name == "Add")).MakeGeneric(serializerEntryTypeCtorRef.DeclaringType);
        var assemblySerializersPerProfileTypeCtorRef = module.ImportReference(assemblySerializersPerProfileType.GetEmptyConstructor());
        var assemblySerializersGetProfilesRef = module.ImportReference(assemblySerializersType.Properties.First(x => x.Name == "Profiles").GetMethod);
        var assemblySerializersGetProfilesSetItemRef = module.ImportReference(assemblySerializersGetProfilesRef.ReturnType.Resolve().Methods.First(x => x.Name == "set_Item"))
            .MakeGeneric([.. ((GenericInstanceType)assemblySerializersGetProfilesRef.ReturnType).GenericArguments]);

        var runtimeHelpersType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(RuntimeHelpers).FullName);
        var runClassConstructorMethod = module.ImportReference(runtimeHelpersType.Methods.Single(x => x.IsPublic && x.Name == "RunClassConstructor" && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.FullName == typeof(RuntimeTypeHandle).FullName));

        foreach (var profile in registry.Context.SerializableTypesProfiles)
        {
            // var profile = new AssemblySerializersPerProfile();
            // assemblySerializers.Profiles["profileKey"] = profile;
            il.Emit(OpCodes.Dup)
              .Emit(OpCodes.Callvirt, assemblySerializersGetProfilesRef)
              .Emit(OpCodes.Ldstr, profile.Key)
              .Emit(OpCodes.Newobj, assemblySerializersPerProfileTypeCtorRef);

            foreach (var type in profile.Value.SerializableTypes.Where(x => x.Value.Local))
            {
                il.Emit(OpCodes.Dup);

                var typeName = type.Key.ConvertCSharp(false);
                var typeId = ObjectId.FromBytes(Encoding.UTF8.GetBytes(typeName));

                unsafe
                {
                        var typeIdHash = (int*)&typeId;

                        for (int i = 0; i < ObjectId.HashSize / 4; ++i)
                            il.Emit(OpCodes.Ldc_I4, typeIdHash[i]);
                    }

                il.Emit(OpCodes.Newobj, objectIdCtorRef)
                  .EmitTypeof(il.Import(type.Key), getTypeFromHandleRef);

                if (type.Value.SerializerType != null)
                {
                    il.EmitTypeof(il.Import(type.Value.SerializerType), getTypeFromHandleRef);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }

                il.Emit(OpCodes.Newobj, serializerEntryTypeCtorRef)
                  .Emit(OpCodes.Callvirt, assemblySerializersPerProfileTypeAddRef);

                if (type.Value.SerializerType?.Resolve()?.Methods.Any(x => x.IsConstructor && x.IsStatic) == true)
                {
                    // RuntimeHelpers.RunClassConstructor(typeof(SerializerType).TypeHandle);
                    il.EmitTypeHandle(type.Value.SerializerType, getTypeFromHandleRef, getTypeHandleMethodRef)
                      .Emit(OpCodes.Call, runClassConstructorMethod);
                }
            }

            il.Emit(OpCodes.Callvirt, assemblySerializersGetProfilesSetItemRef);
        }

        // DataSerializerFactory.RegisterSerializationAssembly(assemblySerializers);
        var dataSerializerFactoryRegisterRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.DataSerializerFactory").Methods.Single(x => x.Name == "RegisterSerializationAssembly" && x.Parameters[0].ParameterType.FullName == assemblySerializersType.FullName));
        il.Emit(OpCodes.Call, dataSerializerFactoryRegisterRef);

        // AssemblyRegistry.Register(typeof(Factory).GetTypeInfo().Assembly, new[] { AssemblyCommonCategories.Engine });
        il.EmitTypeofAssembly(serializerFactoryType, getTypeFromHandleRef, getTypeInfoRef, getAssemblyRef)
          .Emit(OpCodes.Ldc_I4_1)
          .Emit(OpCodes.Newarr, module.TypeSystem.String)
          .Emit(OpCodes.Dup)
          .Emit(OpCodes.Ldc_I4_0)
          .Emit(OpCodes.Ldstr, Reflection.AssemblyCommonCategories.Engine)
          .Emit(OpCodes.Stelem_Ref);

        var assemblyRegistryRegisterMethodRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Reflection.AssemblyRegistry").Methods.Single(x => x.Name == "Register" && x.Parameters[1].ParameterType.IsArray));
        il.Emit(OpCodes.Call, assemblyRegistryRegisterMethodRef)
          .Emit(OpCodes.Ret);

        // Wire up module constructor to call Initialize
        var moduleConstructor = assembly.OpenModuleConstructor(out var returnInstruction);
        var moduleCtorIL = moduleConstructor.Body.GetILProcessor();
        var callInitializeInstruction = moduleCtorIL.Create(OpCodes.Call, module.ImportReference(initializeMethod));
        moduleCtorIL.InsertBefore(moduleConstructor.Body.Instructions.Last(), callInitializeInstruction);
    }

    private static void RegisterDefaultSerializationProfile(IAssemblyResolver assemblyResolver, AssemblyDefinition assembly, ComplexSerializerRegistry registry, System.IO.TextWriter log)
    {
        var mscorlibAssembly = CecilExtensions.FindCorlibAssembly(assembly);
        if (mscorlibAssembly == null)
        {
            log.WriteLine("Missing mscorlib.dll from assembly {0}", assembly.FullName);
            throw new InvalidOperationException("Missing mscorlib.dll from assembly");
        }

        var coreSerializationAssembly = assemblyResolver.Resolve(new AssemblyNameReference("Stride.Core", null));

        // Register serializer factories (determine which type requires which serializer)
        registry.SerializerFactories.Add(new CecilGenericSerializerFactory(typeof(IList<>), coreSerializationAssembly.MainModule.GetTypeResolved("Stride.Core.Serialization.Serializers.ListInterfaceSerializer`1")));
        registry.SerializerFactories.Add(new CecilGenericSerializerFactory(typeof(List<>), coreSerializationAssembly.MainModule.GetTypeResolved("Stride.Core.Serialization.Serializers.ListSerializer`1")));
        registry.SerializerFactories.Add(new CecilGenericSerializerFactory(typeof(KeyValuePair<,>), coreSerializationAssembly.MainModule.GetTypeResolved("Stride.Core.Serialization.Serializers.KeyValuePairSerializer`2")));
        registry.SerializerFactories.Add(new CecilGenericSerializerFactory(typeof(IDictionary<,>), coreSerializationAssembly.MainModule.GetTypeResolved("Stride.Core.Serialization.Serializers.DictionaryInterfaceSerializer`2")));
        registry.SerializerFactories.Add(new CecilGenericSerializerFactory(typeof(Dictionary<,>), coreSerializationAssembly.MainModule.GetTypeResolved("Stride.Core.Serialization.Serializers.DictionarySerializer`2")));
        registry.SerializerFactories.Add(new CecilGenericSerializerFactory(typeof(Nullable<>), coreSerializationAssembly.MainModule.GetTypeResolved("Stride.Core.Serialization.Serializers.NullableSerializer`1")));
        registry.SerializerFactories.Add(new CecilEnumSerializerFactory(coreSerializationAssembly.MainModule.GetTypeResolved("Stride.Core.Serialization.Serializers.EnumSerializer`1")));
        registry.SerializerFactories.Add(new CecilArraySerializerFactory(coreSerializationAssembly.MainModule.GetTypeResolved("Stride.Core.Serialization.Serializers.ArraySerializer`1")));

        // Iterate over tuple size
        for (int i = 1; i <= 4; ++i)
        {
            registry.SerializerDependencies.Add(new CecilSerializerDependency(
                                                     string.Format("System.Tuple`{0}", i),
                                                     coreSerializationAssembly.MainModule.GetTypeResolved(string.Format("Stride.Core.Serialization.Serializers.TupleSerializer`{0}", i))));

            registry.SerializerDependencies.Add(new CecilSerializerDependency(string.Format("Stride.Core.Serialization.Serializers.TupleSerializer`{0}", i)));
        }

        // Register serializer dependencies (determine which serializer serializes which sub-type)
        registry.SerializerDependencies.Add(new CecilSerializerDependency("Stride.Core.Serialization.Serializers.ArraySerializer`1"));
        registry.SerializerDependencies.Add(new CecilSerializerDependency("Stride.Core.Serialization.Serializers.KeyValuePairSerializer`2"));
        registry.SerializerDependencies.Add(new CecilSerializerDependency("Stride.Core.Serialization.Serializers.ListSerializer`1"));
        registry.SerializerDependencies.Add(new CecilSerializerDependency("Stride.Core.Serialization.Serializers.ListInterfaceSerializer`1"));
        registry.SerializerDependencies.Add(new CecilSerializerDependency("Stride.Core.Serialization.Serializers.NullableSerializer`1"));
        registry.SerializerDependencies.Add(new CecilSerializerDependency("Stride.Core.Serialization.Serializers.DictionarySerializer`2",
                                                                           mscorlibAssembly.MainModule.GetTypeResolved(typeof(KeyValuePair<,>).FullName)));
        registry.SerializerDependencies.Add(new CecilSerializerDependency("Stride.Core.Serialization.Serializers.DictionaryInterfaceSerializer`2",
                                                                           mscorlibAssembly.MainModule.GetTypeResolved(typeof(KeyValuePair<,>).FullName)));
    }
}

public static class HashExtensions
{
    public static unsafe void Write(this ObjectIdBuilder objectIdBuilder, int i)
    {
        objectIdBuilder.Write((byte*)&i, sizeof(int));
    }
    public static unsafe void Write(this ObjectIdBuilder objectIdBuilder, bool b)
    {
        objectIdBuilder.WriteByte((byte)(b ? 1 : 0));
    }
}
