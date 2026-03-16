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

/// <summary>
/// Bundles per-descriptor state used during serializer code generation.
/// </summary>
internal struct SerializerCodegenContext
{
    public TypeDefinition Type;
    public TypeDefinition SerializerType;
    public TypeReference[] GenericParameters;
    public TypeReference TypeWithGenerics;
    public SerializationHelpers.SerializableItem[] SerializableItems;
    public Dictionary<TypeReference, (FieldDefinition SerializerField, TypeReference Type)> SerializableItemInfos;
    public Dictionary<TypeReference, VariableDefinition> LocalsByTypes;
    public TypeReference ParentType;
    public FieldDefinition ParentSerializerField;
    public ModuleDefinition Module;
}

internal class SerializationProcessor : IAssemblyDefinitionProcessor
{
    public bool Process(AssemblyProcessorContext context)
    {
        var serializerContext = new CecilSerializerContext(context.Platform, context.Assembly, context.Log);

        // Generate serializer code using Cecil and ILBuilder
        GenerateSerializerCode(serializerContext, out var serializationHash);

        context.SerializationHash = serializationHash;

        return true;
    }

    /// <summary>
    /// Generates serializer code using Cecil and <see cref="ILBuilder"/> for readable IL emission.
    /// </summary>
    private static void GenerateSerializerCode(CecilSerializerContext serializerContext, out ObjectId serializationHash)
    {
        var hash = new ObjectIdBuilder();

        // First, hash global binary format version, in case it gets bumped
        hash.Write(DataSerializer.BinaryFormatVersion);

        var assembly = serializerContext.Assembly;
        var module = assembly.MainModule;
        var strideCoreModule = assembly.GetStrideCoreModule();

        // Generate serializer classes for each pending type
        GenerateSerializerTypes(serializerContext, module, strideCoreModule, hash);

        // Generate the factory type with attributes and module initializer
        GenerateSerializerFactory(serializerContext, assembly, module, strideCoreModule);

        serializationHash = hash.ComputeHash();
    }

    /// <summary>
    /// Creates the <see cref="TypeDefinition"/> for a serializer from its <see cref="SerializerDescriptor"/>,
    /// adds it to the module, and wires it into the <see cref="CecilSerializerContext.SerializableTypeInfo"/>.
    /// </summary>
    private static TypeDefinition CreateSerializerTypeDefinition(
        SerializerDescriptor descriptor,
        ModuleDefinition module,
        ModuleDefinition strideCoreModule)
    {
        var type = descriptor.DataType;
        var serializerType = new TypeDefinition("Stride.Core.DataSerializers", descriptor.SerializerClassName,
            TypeAttributes.AnsiClass | TypeAttributes.AutoClass | TypeAttributes.Sealed |
            TypeAttributes.BeforeFieldInit |
            (descriptor.IsPublic ? TypeAttributes.Public : TypeAttributes.NotPublic));

        // Clone generic parameters from the data type
        if (type.HasGenericParameters)
        {
            foreach (var genericParameter in type.GenericParameters)
            {
                var newGenericParameter = new GenericParameter(genericParameter.Name, serializerType)
                {
                    Attributes = genericParameter.Attributes
                };

                foreach (var constraint in genericParameter.Constraints)
                    newGenericParameter.Constraints.Add(constraint);

                serializerType.GenericParameters.Add(newGenericParameter);
            }
        }

        // Setup base class
        var baseSerializerName = descriptor.UseClassDataSerializer
            ? "Stride.Core.Serialization.ClassDataSerializer`1"
            : "Stride.Core.Serialization.DataSerializer`1";
        var classDataSerializerType = strideCoreModule.GetType(baseSerializerName);
        var parentType = module.ImportReference(classDataSerializerType)
            .MakeGenericType(type.MakeGenericType(serializerType.GenericParameters.ToArray<TypeReference>()));
        serializerType.BaseType = parentType;

        module.Types.Add(serializerType);

        // Update the SerializableTypeInfo to point to the real TypeDefinition
        descriptor.SerializableTypeInfo.SerializerType = serializerType;

        return serializerType;
    }

    /// <summary>
    /// Generates serializer classes (constructor, Initialize, Serialize methods) for each pending type.
    /// </summary>
    private static void GenerateSerializerTypes(
        CecilSerializerContext serializerContext,
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

        foreach (var descriptor in serializerContext.PendingSerializers)
        {
            var type = descriptor.DataType;
            var serializerType = CreateSerializerTypeDefinition(descriptor, module, strideCoreModule);
            var genericParameters = serializerType.GenericParameters.ToArray<TypeReference>();
            var typeWithGenerics = type.MakeGenericType(genericParameters);

            // Hash
            hash.Write(typeWithGenerics.FullName);

            var ctx = new SerializerCodegenContext
            {
                Type = type,
                SerializerType = serializerType,
                GenericParameters = genericParameters,
                TypeWithGenerics = typeWithGenerics,
                SerializableItems = descriptor.SerializableItems,
                SerializableItemInfos = new Dictionary<TypeReference, (FieldDefinition SerializerField, TypeReference Type)>(TypeReferenceEqualityComparer.Default),
                LocalsByTypes = new Dictionary<TypeReference, VariableDefinition>(TypeReferenceEqualityComparer.Default),
                Module = module,
            };

            if (descriptor.SerializedParentType != null)
            {
                ctx.ParentType = descriptor.SerializedParentType;
                ctx.ParentSerializerField = new FieldDefinition("parentSerializer", Mono.Cecil.FieldAttributes.Private, dataSerializerTypeRef.MakeGenericType(ctx.ParentType));
                serializerType.Fields.Add(ctx.ParentSerializerField);

                hash.Write("parent");
            }

            var genericResolver = ResolveGenericsVisitor.FromMapping(type, serializerType);

            foreach (var serializableItem in ctx.SerializableItems)
            {
                if (ctx.SerializableItemInfos.ContainsKey(serializableItem.Type))
                    continue;

                var serializableItemType = serializableItem.Type;
                if (genericResolver != null)
                    serializableItemType = genericResolver.VisitDynamic(serializableItemType);
                var fieldDefinition = new FieldDefinition($"{Utilities.BuildValidClassName(serializableItemType.FullName)}Serializer", Mono.Cecil.FieldAttributes.Private, dataSerializerTypeRef.MakeGenericType(serializableItemType));
                ctx.SerializableItemInfos.Add(serializableItem.Type, (fieldDefinition, serializableItemType));
                serializerType.Fields.Add(fieldDefinition);

                hash.Write(serializableItem.Type.FullName);
                hash.Write(serializableItem.Name);
                hash.Write(serializableItem.AssignBack);
            }

            // Generates: public TypeSerializer() : base() { }
            var ctor = new MethodDefinition(".ctor",
                MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig |
                MethodAttributes.Public, module.TypeSystem.Void);
            var ctorIL = new ILBuilder(ctor.Body, module);
            ctorIL.Emit(OpCodes.Ldarg_0)
                  .Emit(OpCodes.Call, ctorIL.Import(serializerType.BaseType.Resolve().GetEmptyConstructor(true)).MakeGeneric(typeWithGenerics))
                  .Emit(OpCodes.Ret);
            serializerType.Methods.Add(ctor);

            // Generates:
            //   public override void Initialize(SerializerSelector serializerSelector)
            //   {
            //       parentSerializer = serializerSelector.GetSerializer<ParentType>();  // if has parent
            //       field1Serializer = MemberSerializer<Field1Type>.Create(serializerSelector, true);
            //       field2Serializer = MemberSerializer<Field2Type>.Create(serializerSelector, true);
            //       ...
            //   }
            var initialize = new MethodDefinition("Initialize", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, module.TypeSystem.Void);
            initialize.Parameters.Add(new ParameterDefinition("serializerSelector", ParameterAttributes.None, serializerSelectorTypeRef));
            var initIL = new ILBuilder(initialize.Body, module);
            if (ctx.ParentType != null)
            {
                // this.parentSerializer = serializerSelector.GetSerializer<ParentType>();
                initIL.Emit(OpCodes.Ldarg_0)
                      .Emit(OpCodes.Ldarg_1)
                      .Emit(OpCodes.Callvirt, serializerSelectorGetSerializerRef.MakeGenericMethod(ctx.ParentType))
                      .Emit(OpCodes.Stfld, ctx.ParentSerializerField.MakeGeneric(genericParameters));
            }
            foreach (var serializableItem in ctx.SerializableItemInfos)
            {
                // this.fieldSerializer = MemberSerializer<FieldType>.Create(serializerSelector, true);
                initIL.Emit(OpCodes.Ldarg_0)
                      .Emit(OpCodes.Ldarg_1)
                      .Emit(OpCodes.Ldc_I4_1)
                      .Emit(OpCodes.Call, memberSerializerCreateRef.MakeGeneric(serializableItem.Value.Type))
                      .Emit(OpCodes.Stfld, serializableItem.Value.SerializerField.MakeGeneric(genericParameters));
            }
            initIL.Emit(OpCodes.Ret);
            serializerType.Methods.Add(initialize);

            // Add Serialize method
            GenerateSerializeMethod(ctx, dataSerializerSerializeMethod, dataSerializerSerializeMethodRef);
        }
    }

    /// <summary>
    /// Generates the Serialize method for a complex serializer type.
    /// </summary>
    /// <remarks>
    /// Generates code equivalent to:
    /// <code>
    /// public override void Serialize(ref T obj, ArchiveMode mode, SerializationStream stream)
    /// {
    ///     parentSerializer.Serialize(ref obj, mode, stream);  // if has parent
    ///     if (mode == ArchiveMode.Serialize)
    ///     {
    ///         // For each member (field or property):
    ///         var tmp = obj.Member;
    ///         memberSerializer.Serialize(ref tmp, mode, stream);
    ///     }
    ///     else
    ///     {
    ///         // For each member:
    ///         var tmp = default(MemberType);
    ///         memberSerializer.Serialize(ref tmp, mode, stream);
    ///         obj.Member = tmp;  // assign back
    ///     }
    /// }
    /// </code>
    /// For fields with AssignBack, the field address is used directly (no temp variable).
    /// </remarks>
    private static void GenerateSerializeMethod(
        SerializerCodegenContext ctx,
        MethodDefinition dataSerializerSerializeMethod,
        MethodReference dataSerializerSerializeMethodRef)
    {
        var module = ctx.Module;
        var serialize = new MethodDefinition("Serialize", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, module.TypeSystem.Void);
        serialize.Parameters.Add(new ParameterDefinition("obj", ParameterAttributes.None, ctx.TypeWithGenerics.MakeByReferenceType()));
        // Copy other parameters from parent method
        for (int i = 1; i < dataSerializerSerializeMethod.Parameters.Count; ++i)
        {
            var parentParameter = dataSerializerSerializeMethod.Parameters[i];
            serialize.Parameters.Add(new ParameterDefinition(parentParameter.Name, ParameterAttributes.None, module.ImportReference(parentParameter.ParameterType)));
        }

        var il = new ILBuilder(serialize.Body, module);

        // this.parentSerializer.Serialize(ref obj, mode, stream);
        if (ctx.ParentType != null)
        {
            il.Emit(OpCodes.Ldarg_0)
              .Emit(OpCodes.Ldfld, ctx.ParentSerializerField.MakeGeneric(ctx.GenericParameters))
              .Emit(OpCodes.Ldarg_1)
              .Emit(OpCodes.Ldarg_2)
              .Emit(OpCodes.Ldarg_3)
              .Emit(OpCodes.Callvirt, dataSerializerSerializeMethodRef.MakeGeneric(ctx.ParentType));
        }

        if (ctx.SerializableItems.Length > 0)
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

                foreach (var serializableItem in ctx.SerializableItems)
                {
                    if (serializableItem.HasFixedAttribute)
                    {
                        throw new NotImplementedException("FixedBuffer attribute is not supported.");
                    }

                    var memberAssignBack = serializableItem.AssignBack;
                    var memberVariableName = (serializableItem.MemberInfo is PropertyDefinition || !memberAssignBack) ? SerializationHelpers.CreateMemberVariableName(serializableItem.MemberInfo) : null;
                    var serializableItemInfo = ctx.SerializableItemInfos[serializableItem.Type];
                    il.Emit(OpCodes.Ldarg_0)
                      .Emit(OpCodes.Ldfld, serializableItemInfo.SerializerField.MakeGeneric(ctx.GenericParameters));

                    var fieldReference = serializableItem.MemberInfo is FieldReference ? il.Import((FieldReference)serializableItem.MemberInfo).MakeGeneric(ctx.GenericParameters) : null;

                    if (memberVariableName != null)
                    {
                        // Properties (and non-assignback fields) need a temp variable:
                        //   var tmp = obj.Member;           // serialize path
                        //   var tmp = default(MemberType);  // deserialize path
                        if (!ctx.LocalsByTypes.TryGetValue(serializableItemInfo.Type, out var tempLocal))
                        {
                            tempLocal = il.AddLocal(serializableItemInfo.Type);
                            ctx.LocalsByTypes.Add(serializableItemInfo.Type, tempLocal);
                        }

                        if (!(archiveMode == ArchiveMode.Deserialize && memberAssignBack))
                        {
                            // var tmp = obj.Member;
                            il.Emit(OpCodes.Ldarg_1);
                            if (!ctx.Type.IsValueType)
                                il.Emit(OpCodes.Ldind_Ref);

                            EmitLoadMember(il, serializableItem, fieldReference, ctx.GenericParameters);
                            il.Emit(OpCodes.Stloc, tempLocal)
                              .Emit(OpCodes.Ldloca, tempLocal);
                        }
                        else
                        {
                            // var tmp = default(MemberType);
                            il.Emit(OpCodes.Ldloca, tempLocal)
                              .Emit(OpCodes.Dup)
                              .Emit(OpCodes.Initobj, serializableItemInfo.Type);
                        }
                    }
                    else
                    {
                        // Field with AssignBack: pass field address directly — &obj.field
                        il.Emit(OpCodes.Ldarg_1);
                        if (!ctx.Type.IsValueType)
                            il.Emit(OpCodes.Ldind_Ref);
                        il.Emit(OpCodes.Ldflda, fieldReference);
                    }
                    // memberSerializer.Serialize(ref tmp, mode, stream);
                    il.Emit(OpCodes.Ldarg_2)
                      .Emit(OpCodes.Ldarg_3)
                      .Emit(OpCodes.Callvirt, dataSerializerSerializeMethodRef.MakeGeneric(serializableItemInfo.Type));

                    if (archiveMode == ArchiveMode.Deserialize && memberVariableName != null && memberAssignBack)
                    {
                        // obj.Member = tmp;
                        il.Emit(OpCodes.Ldarg_1);
                        if (!ctx.Type.IsValueType)
                            il.Emit(OpCodes.Ldind_Ref);

                        il.Emit(OpCodes.Ldloc, ctx.LocalsByTypes[serializableItemInfo.Type]);
                        EmitStoreMember(il, serializableItem, fieldReference, ctx.GenericParameters);
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
        ctx.SerializerType.Methods.Add(serialize);
    }

    private static void EmitLoadMember(ILBuilder il, SerializationHelpers.SerializableItem item, FieldReference fieldReference, TypeReference[] genericParameters)
    {
        if (item.MemberInfo is PropertyDefinition property)
        {
            var getMethod = property.Resolve().GetMethod;
            il.Emit(getMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, il.Import(getMethod).MakeGeneric(genericParameters));
        }
        else if (item.MemberInfo is FieldDefinition)
        {
            il.Emit(OpCodes.Ldfld, fieldReference);
        }
    }

    private static void EmitStoreMember(ILBuilder il, SerializationHelpers.SerializableItem item, FieldReference fieldReference, TypeReference[] genericParameters)
    {
        if (item.MemberInfo is PropertyDefinition property)
        {
            var setMethod = property.Resolve().SetMethod;
            il.Emit(setMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, il.Import(setMethod).MakeGeneric(genericParameters));
        }
        else if (item.MemberInfo is FieldDefinition)
        {
            il.Emit(OpCodes.Stfld, fieldReference);
        }
    }

    /// <summary>
    /// Generates the serializer factory type with <see cref="DataSerializerGlobalAttribute"/>s
    /// and the module initializer that registers all serializers at runtime.
    /// </summary>
    private static void GenerateSerializerFactory(
        CecilSerializerContext serializerContext,
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
        EmitDataSerializerGlobalAttributes(serializerContext, module, strideCoreModule, typeTypeRef, serializerFactoryType);

        // Generate the Initialize method (module initializer body)
        GenerateInitializeMethod(serializerContext, assembly, module, strideCoreModule, serializerFactoryType);

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
        CecilSerializerContext serializerContext,
        ModuleDefinition module,
        ModuleDefinition strideCoreModule,
        TypeReference typeTypeRef,
        TypeDefinition serializerFactoryType)
    {
        var dataSerializerModeTypeRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.DataSerializerGenericMode"));
        var dataSerializerGlobalAttribute = strideCoreModule.GetType("Stride.Core.Serialization.DataSerializerGlobalAttribute");
        var dataSerializerGlobalCtorRef = module.ImportReference(dataSerializerGlobalAttribute.GetConstructors().Single(x => !x.IsStatic && x.Parameters.Count == 5));

        foreach (var profile in serializerContext.SerializableTypesProfiles)
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
                        new CustomAttributeArgument(module.TypeSystem.Boolean, type.Value.IsGeneratedSerializer),
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
    /// <remarks>
    /// Generates code equivalent to:
    /// <code>
    /// static void Initialize()
    /// {
    ///     var assemblySerializers = new AssemblySerializers(typeof(Factory).GetTypeInfo().Assembly);
    ///     // Register DataContract aliases
    ///     assemblySerializers.DataContractAliases.Add(new DataContractAlias("alias", typeof(T), isRemap));
    ///     // Register referenced modules
    ///     assemblySerializers.Modules.Add(typeof(RefFactory).GetTypeInfo().Module);
    ///     // Register serializer entries per profile
    ///     var profile = new AssemblySerializersPerProfile();
    ///     profile.Add(new AssemblySerializerEntry(objectId, typeof(T), typeof(TSerializer)));
    ///     assemblySerializers.Profiles["Default"] = profile;
    ///     DataSerializerFactory.RegisterSerializationAssembly(assemblySerializers);
    ///     AssemblyRegistry.Register(typeof(Factory).GetTypeInfo().Assembly, new[] { "Engine" });
    /// }
    /// </code>
    /// </remarks>
    private static void GenerateInitializeMethod(
        CecilSerializerContext serializerContext,
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

        // Emit: var assemblySerializers = new AssemblySerializers(typeof(Factory).GetTypeInfo().Assembly);
        var assemblySerializersType = strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializers");
        il.EmitTypeofAssembly(serializerFactoryType, getTypeFromHandleRef, getTypeInfoRef, getAssemblyRef)
          .Emit(OpCodes.Newobj, il.Import(assemblySerializersType.Methods.Single(x => x.IsConstructor && x.Parameters.Count == 1)));

        EmitDataContractAliases(il, serializerContext, module, assemblySerializersType, getTypeFromHandleRef);
        EmitModuleRegistrations(il, serializerContext, module, assemblySerializersType, getTypeFromHandleRef, getTypeInfoRef, getModuleRef);
        EmitProfileEntries(il, serializerContext, module, strideCoreModule, mscorlibAssembly, assemblySerializersType, getTypeFromHandleRef, getTypeHandleMethodRef);

        // Emit: DataSerializerFactory.RegisterSerializationAssembly(assemblySerializers);
        var dataSerializerFactoryRegisterRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.DataSerializerFactory").Methods.Single(x => x.Name == "RegisterSerializationAssembly" && x.Parameters[0].ParameterType.FullName == assemblySerializersType.FullName));
        il.Emit(OpCodes.Call, dataSerializerFactoryRegisterRef);

        // Emit: AssemblyRegistry.Register(typeof(Factory).GetTypeInfo().Assembly, new[] { "Engine" });
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

    /// <summary>
    /// Emits: <c>assemblySerializers.DataContractAliases.Add(new (alias, typeof(T), isRemap));</c> for each alias.
    /// </summary>
    private static void EmitDataContractAliases(
        ILBuilder il, CecilSerializerContext serializerContext, ModuleDefinition module,
        TypeDefinition assemblySerializersType, MethodReference getTypeFromHandleRef)
    {
        var getAliasesRef = module.ImportReference(assemblySerializersType.Properties.First(x => x.Name == "DataContractAliases").GetMethod);
        var addMethod = getAliasesRef.ReturnType.Resolve().Methods.First(x => x.Name == "Add");
        var aliasTypeRef = ((GenericInstanceType)getAliasesRef.ReturnType).GenericArguments[0];
        var aliasCtorRef = module.ImportReference(aliasTypeRef.Resolve().GetConstructors().Single());
        var addRef = module.ImportReference(addMethod).MakeGeneric(aliasTypeRef);

        foreach (var alias in serializerContext.DataContractAliases)
        {
            il.Emit(OpCodes.Dup)
              .Emit(OpCodes.Call, getAliasesRef)
              .Emit(OpCodes.Ldstr, alias.Item1)
              .EmitTypeof(il.Import(alias.Item2), getTypeFromHandleRef)
              .Emit(alias.Item3 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0)
              .Emit(OpCodes.Newobj, aliasCtorRef)
              .Emit(OpCodes.Call, addRef);
        }
    }

    /// <summary>
    /// Emits: <c>assemblySerializers.Modules.Add(typeof(RefFactory).GetTypeInfo().Module);</c> for each referenced assembly.
    /// </summary>
    private static void EmitModuleRegistrations(
        ILBuilder il, CecilSerializerContext serializerContext, ModuleDefinition module,
        TypeDefinition assemblySerializersType,
        MethodReference getTypeFromHandleRef, MethodReference getTypeInfoRef, MethodReference getModuleRef)
    {
        var getModulesRef = module.ImportReference(assemblySerializersType.Properties.First(x => x.Name == "Modules").GetMethod);
        var addMethod = getModulesRef.ReturnType.Resolve().Methods.First(x => x.Name == "Add");
        var moduleTypeRef = ((GenericInstanceType)getModulesRef.ReturnType).GenericArguments[0];
        var addRef = module.ImportReference(addMethod).MakeGeneric(moduleTypeRef);

        foreach (var referencedAssemblySerializerFactoryType in serializerContext.ReferencedAssemblySerializerFactoryTypes)
        {
            il.Emit(OpCodes.Dup)
              .Emit(OpCodes.Call, getModulesRef)
              .EmitTypeofModule(il.Import(referencedAssemblySerializerFactoryType), getTypeFromHandleRef, getTypeInfoRef, getModuleRef)
              .Emit(OpCodes.Call, addRef);
        }
    }

    /// <summary>
    /// Emits per-profile serializer registration:
    /// <code>
    /// var profile = new AssemblySerializersPerProfile();
    /// profile.Add(new AssemblySerializerEntry(typeId, typeof(T), typeof(TSerializer)));
    /// RuntimeHelpers.RunClassConstructor(typeof(TSerializer).TypeHandle);  // if has static ctor
    /// assemblySerializers.Profiles["Default"] = profile;
    /// </code>
    /// </summary>
    private static void EmitProfileEntries(
        ILBuilder il, CecilSerializerContext serializerContext, ModuleDefinition module,
        ModuleDefinition strideCoreModule, AssemblyDefinition mscorlibAssembly,
        TypeDefinition assemblySerializersType,
        MethodReference getTypeFromHandleRef, MethodReference getTypeHandleMethodRef)
    {
        var objectIdCtorRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Storage.ObjectId").GetConstructors().Single(x => x.Parameters.Count == 4));
        var serializerEntryTypeCtorRef = module.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializerEntry").GetConstructors().Single());
        var perProfileType = strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializersPerProfile");
        var perProfileAddRef = module.ImportReference(perProfileType.BaseType.Resolve().Methods.First(x => x.Name == "Add")).MakeGeneric(serializerEntryTypeCtorRef.DeclaringType);
        var perProfileCtorRef = module.ImportReference(perProfileType.GetEmptyConstructor());
        var getProfilesRef = module.ImportReference(assemblySerializersType.Properties.First(x => x.Name == "Profiles").GetMethod);
        var setItemRef = module.ImportReference(getProfilesRef.ReturnType.Resolve().Methods.First(x => x.Name == "set_Item"))
            .MakeGeneric([.. ((GenericInstanceType)getProfilesRef.ReturnType).GenericArguments]);

        var runtimeHelpersType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(RuntimeHelpers).FullName);
        var runClassConstructorMethod = module.ImportReference(runtimeHelpersType.Methods.Single(x => x.IsPublic && x.Name == "RunClassConstructor" && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.FullName == typeof(RuntimeTypeHandle).FullName));

        foreach (var profile in serializerContext.SerializableTypesProfiles)
        {
            il.Emit(OpCodes.Dup)
              .Emit(OpCodes.Callvirt, getProfilesRef)
              .Emit(OpCodes.Ldstr, profile.Key)
              .Emit(OpCodes.Newobj, perProfileCtorRef);

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
                  .Emit(OpCodes.Callvirt, perProfileAddRef);

                if (type.Value.SerializerType?.Resolve()?.Methods.Any(x => x.IsConstructor && x.IsStatic) == true)
                {
                    // RuntimeHelpers.RunClassConstructor(typeof(SerializerType).TypeHandle);
                    il.EmitTypeHandle(type.Value.SerializerType, getTypeFromHandleRef, getTypeHandleMethodRef)
                      .Emit(OpCodes.Call, runClassConstructorMethod);
                }
            }

            il.Emit(OpCodes.Callvirt, setItemRef);
        }
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
