// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Stride.Core.AssemblyProcessor.Serializers;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Stride.Core.AssemblyProcessor;

internal partial class UpdateEngineProcessor : ICecilSerializerProcessor
{
    private MethodDefinition updatableFieldGenericCtor;
    private MethodDefinition updatableListUpdateResolverGenericCtor;
    private MethodDefinition updatableArrayUpdateResolverGenericCtor;

    private TypeDefinition updatablePropertyGenericType;
    private MethodDefinition updatablePropertyGenericCtor;
    private MethodDefinition updatablePropertyObjectGenericCtor;

    private MethodDefinition parameterCollectionResolverInstantiateValueAccessor;

    private MethodReference updateEngineRegisterMemberMethod;
    private MethodReference updateEngineRegisterMemberResolverMethod;

    private MethodReference getTypeFromHandleMethod;

    int prepareMethodCount = 0;

    private MethodDefinition CreateUpdateMethod(AssemblyDefinition assembly)
    {
        // Get or create method
        var updateEngineType = GetOrCreateUpdateType(assembly, true);
        var mainPrepareMethod = new MethodDefinition($"UpdateMain{prepareMethodCount++}", MethodAttributes.HideBySig | MethodAttributes.Assembly | MethodAttributes.Static, assembly.MainModule.TypeSystem.Void);
        updateEngineType.Methods.Add(mainPrepareMethod);

        // Obtain the static constructor of <Module> and the return instruction
        var moduleConstructor = assembly.OpenModuleConstructor(out var returnInstruction);

        // Get the IL processor of the module constructor (used only for InsertBefore)
        var moduleCtorIL = moduleConstructor.Body.GetILProcessor();

        // Create the call to Initialize method
        var initializeMethodReference = assembly.MainModule.ImportReference(mainPrepareMethod);
        var callInitializeInstruction = moduleCtorIL.Create(OpCodes.Call, initializeMethodReference);
        moduleCtorIL.InsertBefore(moduleConstructor.Body.Instructions.Last(), callInitializeInstruction);

        return mainPrepareMethod;
    }

    /// <summary>
    /// Creates a static dispatcher method for virtual/interface property access via <c>ldvirtftn</c>/<c>calli</c>.
    /// </summary>
    /// <remarks>
    /// Generates code equivalent to:
    /// <code>
    /// static TReturn Dispatcher_get_Property(object @this /*, params... */)
    /// {
    ///     return @this./* virtual call via ldvirtftn+calli */get_Property(/* params... */);
    /// }
    /// </code>
    /// This is needed because <c>ldftn</c> of a virtual method gives the base slot, not the override.
    /// The dispatcher uses <c>ldvirtftn</c> to resolve the actual vtable entry at runtime.
    /// </remarks>
    private MethodReference CreateDispatcher(AssemblyDefinition assembly, MethodReference method)
    {
        var updateEngineType = GetOrCreateUpdateType(assembly, true);
        var module = assembly.MainModule;

        var dispatcherMethod = new MethodDefinition($"Dispatcher_{method.Name}", MethodAttributes.HideBySig | MethodAttributes.Assembly | MethodAttributes.Static, module.ImportReference(method.ReturnType));
        updateEngineType.Methods.Add(dispatcherMethod);

        dispatcherMethod.Parameters.Add(new ParameterDefinition("this", ParameterAttributes.None, method.DeclaringType));
        foreach (var param in method.Parameters)
        {
            dispatcherMethod.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
        }

        // Emit: load all args, then resolve virtual method and call via calli
        var il = new ILBuilder(dispatcherMethod.Body, module);
        il.Emit(OpCodes.Ldarg_0);
        // note: first parameter is "this"
        foreach (var param in dispatcherMethod.Parameters.Skip(1))
        {
            il.Emit(OpCodes.Ldarg, param);
        }
        var callsite = new Mono.Cecil.CallSite(method.ReturnType) { HasThis = true };
        foreach (var param in method.Parameters)
            callsite.Parameters.Add(param);
        // Emit: @this.ldvirtftn(method) then calli — resolves the actual override at runtime
        il.Emit(OpCodes.Ldarg_0)
          .Emit(OpCodes.Ldvirtftn, method)
          .Emit(OpCodes.Calli, callsite)
          .Emit(OpCodes.Ret);

        return dispatcherMethod;
    }

    public void ProcessSerializers(CecilSerializerContext context)
    {
        var references = new HashSet<AssemblyDefinition>();
        EnumerateReferences(references, context.Assembly);

        var coreAssembly = CecilExtensions.FindCorlibAssembly(context.Assembly);
        var module = context.Assembly.MainModule;

        // Only process assemblies depending on Stride.Engine
        if (!references.Any(x => x.Name.Name == "Stride.Engine"))
        {
            // Make sure Stride.Engine.Serializers can access everything internally
            var internalsVisibleToAttribute = coreAssembly.MainModule.GetTypeResolved(typeof(InternalsVisibleToAttribute).FullName);
            var serializationAssemblyName = "Stride.Engine.Serializers";

            // Add [InteralsVisibleTo] attribute
            var internalsVisibleToAttributeCtor = module.ImportReference(internalsVisibleToAttribute.GetConstructors().Single());
            var internalsVisibleAttribute = new CustomAttribute(internalsVisibleToAttributeCtor)
            {
                ConstructorArguments =
                        {
                            new CustomAttributeArgument(module.ImportReference(module.TypeSystem.String), serializationAssemblyName)
                        }
            };
            context.Assembly.CustomAttributes.Add(internalsVisibleAttribute);

            return;
        }

        var strideEngineAssembly = context.Assembly.Name.Name == "Stride.Engine"
                ? context.Assembly
                : module.AssemblyResolver.Resolve(new AssemblyNameReference("Stride.Engine", null));
        var strideEngineModule = strideEngineAssembly.MainModule;

        // Generate IL for Stride.Core
        if (context.Assembly.Name.Name == "Stride.Engine")
        {
            ProcessStrideEngineAssembly(context);
        }

        var updatableFieldGenericType = strideEngineModule.GetType("Stride.Updater.UpdatableField`1");
        updatableFieldGenericCtor = updatableFieldGenericType.Methods.First(x => x.IsConstructor && !x.IsStatic);

        updatablePropertyGenericType = strideEngineModule.GetType("Stride.Updater.UpdatableProperty`1");
        updatablePropertyGenericCtor = updatablePropertyGenericType.Methods.First(x => x.IsConstructor && !x.IsStatic);

        var updatablePropertyObjectGenericType = strideEngineModule.GetType("Stride.Updater.UpdatablePropertyObject`1");
        updatablePropertyObjectGenericCtor = updatablePropertyObjectGenericType.Methods.First(x => x.IsConstructor && !x.IsStatic);

        var updatableListUpdateResolverGenericType = strideEngineModule.GetType("Stride.Updater.ListUpdateResolver`1");
        updatableListUpdateResolverGenericCtor = updatableListUpdateResolverGenericType.Methods.First(x => x.IsConstructor && !x.IsStatic);

        var updatableArrayUpdateResolverGenericType = strideEngineModule.GetType("Stride.Updater.ArrayUpdateResolver`1");
        updatableArrayUpdateResolverGenericCtor = updatableArrayUpdateResolverGenericType.Methods.First(x => x.IsConstructor && !x.IsStatic);

        var parameterCollectionResolver = strideEngineModule.GetType("Stride.Engine.Design.ParameterCollectionResolver");
        parameterCollectionResolverInstantiateValueAccessor = parameterCollectionResolver.Methods.First(x => x.Name == "InstantiateValueAccessor");

        var registerMemberMethod = strideEngineModule.GetType("Stride.Updater.UpdateEngine").Methods.First(x => x.Name == "RegisterMember");
        updateEngineRegisterMemberMethod = module.ImportReference(registerMemberMethod);

        var registerMemberResolverMethod = strideEngineModule.GetType("Stride.Updater.UpdateEngine").Methods.First(x => x.Name == "RegisterMemberResolver");
        //pclVisitor.VisitMethod(registerMemberResolverMethod);
        updateEngineRegisterMemberResolverMethod = module.ImportReference(registerMemberResolverMethod);

        var typeType = coreAssembly.MainModule.GetTypeResolved(typeof(Type).FullName);
        getTypeFromHandleMethod = module.ImportReference(typeType.Methods.First(x => x.Name == "GetTypeFromHandle"));

        var mainPrepareMethod = CreateUpdateMethod(context.Assembly);

        // Emit serialization code for all the types we care about
        var processedTypes = new HashSet<TypeDefinition>(TypeReferenceEqualityComparer.Default);
        foreach (var serializableType in context.SerializableTypesProfiles.SelectMany(x => x.Value.SerializableTypes))
        {
            // Special case: when processing Stride.Engine assembly, we automatically add dependent assemblies types too
            if (!serializableType.Value.Local && strideEngineAssembly != context.Assembly)
                continue;

            if (serializableType.Key is not TypeDefinition typeDefinition)
                continue;

            // Ignore already processed types
            if (!processedTypes.Add(typeDefinition))
                continue;

            try
            {
                ProcessType(context, module.ImportReference(typeDefinition), mainPrepareMethod);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(string.Format("Error when generating update engine code for {0}", typeDefinition), e);
            }
        }

        // Force generic instantiations — register resolvers for lists, arrays, and trigger generic update methods
        // Generates calls like:
        //   UpdateEngine.RegisterMemberResolver(new ListUpdateResolver<ElementType>());
        //   UpdateEngine.RegisterMemberResolver(new ArrayUpdateResolver<ElementType>());
        //   ParameterCollectionResolver.InstantiateValueAccessor<KeyType>();  // iOS AOT only
        //   UpdateGeneric_TypeName<T1, T2>();  // for closed generic types
        var il = new ILBuilder(mainPrepareMethod.Body, module);
        foreach (var serializableType in context.SerializableTypesProfiles.SelectMany(x => x.Value.SerializableTypes).ToArray())
        {
            // Special case: when processing Stride.Engine assembly, we automatically add dependent assemblies types too
            if (!serializableType.Value.Local && strideEngineAssembly != context.Assembly)
                continue;

            // Try to find if original method definition was generated
            var typeDefinition = serializableType.Key.Resolve();

            // If using List<T>, register this type in UpdateEngine
            var parentTypeDefinition = typeDefinition;
            while (parentTypeDefinition != null)
            {
                var listInterfaceType = parentTypeDefinition.Interfaces.Select(x => x.InterfaceType).OfType<GenericInstanceType>().FirstOrDefault(x => x.ElementType.FullName == typeof(IList<>).FullName);
                if (listInterfaceType != null)
                {
                    //call Updater.UpdateEngine.RegisterMemberResolver(new Updater.ListUpdateResolver<T>());
                    var elementType = ResolveGenericsVisitor.Process(serializableType.Key, listInterfaceType.GenericArguments[0]);
                    il.Emit(OpCodes.Newobj, il.Import(updatableListUpdateResolverGenericCtor).MakeGeneric(il.Import(elementType)))
                      .Emit(OpCodes.Call, updateEngineRegisterMemberResolverMethod);
                }

                parentTypeDefinition = parentTypeDefinition.BaseType?.Resolve();
            }

            // Same for arrays
            if (serializableType.Key is ArrayType arrayType)
            {
                //call Updater.UpdateEngine.RegisterMemberResolver(new Updater.ArrayUpdateResolver<T>());
                var elementType = ResolveGenericsVisitor.Process(serializableType.Key, arrayType.ElementType);
                il.Emit(OpCodes.Newobj, il.Import(updatableArrayUpdateResolverGenericCtor).MakeGeneric(il.Import(elementType)))
                  .Emit(OpCodes.Call, updateEngineRegisterMemberResolverMethod);
            }

            // Generic instantiation for AOT platforms
            if (context.Platform == Core.PlatformType.iOS && serializableType.Key.Name == "ValueParameterKey`1")
            {
                var keyType = ((GenericInstanceType)serializableType.Key).GenericArguments[0];
                il.Emit(OpCodes.Call, il.Import(parameterCollectionResolverInstantiateValueAccessor).MakeGenericMethod(il.Import(keyType)));
            }

            if (serializableType.Key is GenericInstanceType genericInstanceType)
            {
                var expectedUpdateMethodName = ComputeUpdateMethodName(typeDefinition);
                var updateMethod = GetOrCreateUpdateType(typeDefinition.Module.Assembly, false)?.Methods.FirstOrDefault(x => x.Name == expectedUpdateMethodName && x.HasGenericParameters && x.GenericParameters.Count == genericInstanceType.GenericParameters.Count);

                // If nothing was found in main assembly, also look in Stride.Engine assembly, just in case (it might defines some shared/corlib types -- currently not the case)
                if (updateMethod == null)
                {
                    updateMethod = GetOrCreateUpdateType(strideEngineAssembly, false)?.Methods.FirstOrDefault(x => x.Name == expectedUpdateMethodName && x.HasGenericParameters && x.GenericParameters.Count == genericInstanceType.GenericParameters.Count);
                }

                if (updateMethod != null)
                {
                    // Emit call to update engine setup method with generic arguments of current type
                    il.Emit(OpCodes.Call, il.Import(updateMethod)
                        .MakeGenericMethod(genericInstanceType.GenericArguments
                            .Select(x => module.ImportReference(x))
                            .ToArray()));
                }
            }
        }

        il.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// Registers all updatable fields and properties of a type with the update engine.
    /// For generic types, creates a separate <c>UpdateGeneric_TypeName&lt;T&gt;()</c> method
    /// that can be instantiated per closed generic type.
    /// </summary>
    public void ProcessType(CecilSerializerContext context, TypeReference type, MethodDefinition updateMainMethod)
    {
        var typeDefinition = type.Resolve();

        // No need to process enum
        if (typeDefinition.IsEnum)
            return;

        var module = context.Assembly.MainModule;
        var updateCurrentMethod = updateMainMethod;
        ResolveGenericsVisitor replaceGenericsVisitor = null;

        if (typeDefinition.HasGenericParameters)
        {
            // Make a prepare method for just this object since it might need multiple instantiation
            updateCurrentMethod = new MethodDefinition(ComputeUpdateMethodName(typeDefinition), MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
            foreach (var genericParameter in typeDefinition.GenericParameters)
            {
                var genericParameterCopy = new GenericParameter(genericParameter.Name, updateCurrentMethod)
                {
                    Attributes = genericParameter.Attributes,
                };
                foreach (var constraint in genericParameter.Constraints)
                    genericParameterCopy.Constraints.Add(new GenericParameterConstraint(module.ImportReference(constraint.ConstraintType)));
                updateCurrentMethod.GenericParameters.Add(genericParameterCopy);
            }

            replaceGenericsVisitor = ResolveGenericsVisitor.FromMapping(typeDefinition, updateCurrentMethod);

            updateMainMethod.DeclaringType.Methods.Add(updateCurrentMethod);
        }

        var il = new ILBuilder(updateCurrentMethod.Body, module);
        var typeIsValueType = type.IsResolvedValueType();
        var emptyObjectField = typeIsValueType ? null : updateMainMethod.DeclaringType.Fields.FirstOrDefault(x => x.Name == "emptyObject");
        VariableDefinition emptyStruct = null;

        // Note: forcing fields and properties to be processed in all cases
        foreach (var serializableItem in SerializationHelpers.GetSerializableItems(type, true, ComplexTypeSerializerFlags.SerializePublicFields | ComplexTypeSerializerFlags.SerializePublicProperties | ComplexTypeSerializerFlags.Updatable, context.IgnoredMembers))
        {
            if (serializableItem.MemberInfo is FieldReference fieldReference)
                EmitFieldRegistration(il, fieldReference, type, typeIsValueType, replaceGenericsVisitor, module, updateMainMethod, ref emptyObjectField, ref emptyStruct);

            if (serializableItem.MemberInfo is PropertyReference propertyReference)
                EmitPropertyRegistration(il, propertyReference, type, replaceGenericsVisitor, context.Assembly, updateCurrentMethod);
        }

        if (updateCurrentMethod != updateMainMethod)
        {
            // If we have a local method, close it
            il.Emit(OpCodes.Ret);

            // Also call it from main method if it was a closed generic instantiation
            if (type is GenericInstanceType genericInstanceType)
            {
                var mainIL = new ILBuilder(updateMainMethod.Body, module);
                mainIL.Emit(OpCodes.Call, updateCurrentMethod.MakeGeneric(genericInstanceType.GenericArguments.Select(module.ImportReference).ToArray()));
            }
        }
    }

    /// <summary>
    /// Emits field registration with the update engine using pointer offset computation.
    /// </summary>
    /// <remarks>
    /// Generates code equivalent to:
    /// <code>
    /// // For reference types:
    /// UpdateEngine.RegisterMember(typeof(T), "fieldName",
    ///     new UpdatableField&lt;FieldType&gt;(&amp;emptyObject.field - &amp;emptyObject));
    /// // For value types:
    /// UpdateEngine.RegisterMember(typeof(T), "fieldName",
    ///     new UpdatableField&lt;FieldType&gt;(&amp;emptyStruct.field - &amp;emptyStruct));
    /// </code>
    /// </remarks>
    private void EmitFieldRegistration(
        ILBuilder il, FieldReference fieldReference, TypeReference type, bool typeIsValueType,
        ResolveGenericsVisitor replaceGenericsVisitor, ModuleDefinition module,
        MethodDefinition updateMainMethod, ref FieldDefinition emptyObjectField, ref VariableDefinition emptyStruct)
    {
        var field = fieldReference.Resolve();

        // We need a dummy instance to compute field offsets via pointer math: &empty.field - &empty
        // For reference types: a static field initialized in .cctor (var emptyObject = new object())
        // For value types: a local variable (T emptyStruct)
        if (typeIsValueType)
        {
            if (emptyStruct == null)
            {
                emptyStruct = new VariableDefinition(type);
                updateMainMethod.Body.Variables.Add(emptyStruct);
            }
        }
        else
        {
            if (emptyObjectField == null)
            {
                emptyObjectField = new FieldDefinition("emptyObject", FieldAttributes.Static | FieldAttributes.Private, module.TypeSystem.Object);

                // Create static ctor that will initialize this object
                var staticConstructor = new MethodDefinition(".cctor",
                                        MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                                        module.TypeSystem.Void);
                var staticCtorIL = new ILBuilder(staticConstructor.Body, module);
                staticCtorIL.Emit(OpCodes.Newobj, module.ImportReference(emptyObjectField.FieldType.Resolve().GetConstructors().Single(x => !x.IsStatic && !x.HasParameters)))
                            .Emit(OpCodes.Stsfld, emptyObjectField)
                            .Emit(OpCodes.Ret);

                updateMainMethod.DeclaringType.Fields.Add(emptyObjectField);
                updateMainMethod.DeclaringType.Methods.Add(staticConstructor);
            }
        }

        // Emit: typeof(T), "fieldName"
        il.EmitTypeof(type, getTypeFromHandleMethod)
          .Emit(OpCodes.Ldstr, field.Name);

        // Emit: (int)(&empty.field - &empty) — computes field byte offset
        if (typeIsValueType)
            il.Emit(OpCodes.Ldloca, emptyStruct);
        else
            il.Emit(OpCodes.Ldsfld, emptyObjectField);
        il.Emit(OpCodes.Ldflda, il.Import(fieldReference))
          .Emit(OpCodes.Conv_I);
        if (typeIsValueType)
            il.Emit(OpCodes.Ldloca, emptyStruct);
        else
            il.Emit(OpCodes.Ldsfld, emptyObjectField);
        il.Emit(OpCodes.Conv_I)
          .Emit(OpCodes.Sub)
          .Emit(OpCodes.Conv_I4);

        // Emit: new UpdatableField<FieldType>(offset)
        var fieldType = il.Import(replaceGenericsVisitor != null ? replaceGenericsVisitor.VisitDynamic(field.FieldType) : field.FieldType);
        il.Emit(OpCodes.Newobj, il.Import(updatableFieldGenericCtor).MakeGeneric(fieldType))
          // Emit: UpdateEngine.RegisterMember(typeof(T), "fieldName", updatableField);
          .Emit(OpCodes.Call, updateEngineRegisterMemberMethod);
    }

    /// <summary>
    /// Emits property registration with the update engine using function pointers.
    /// </summary>
    /// <remarks>
    /// Generates code equivalent to:
    /// <code>
    /// UpdateEngine.RegisterMember(typeof(T), "PropertyName",
    ///     new UpdatableProperty&lt;PropType&gt;(
    ///         ldftn(get_Property),      // or ldftn(Dispatcher_get_Property) for virtual
    ///         isGetterVirtual,
    ///         ldftn(set_Property),      // or IntPtr.Zero if no public setter
    ///         isSetterVirtual));
    /// </code>
    /// Virtual/interface properties use a dispatcher trampoline (see <see cref="CreateDispatcher"/>).
    /// </remarks>
    private void EmitPropertyRegistration(
        ILBuilder il, PropertyReference propertyReference, TypeReference type,
        ResolveGenericsVisitor replaceGenericsVisitor, AssemblyDefinition assembly,
        MethodDefinition updateCurrentMethod)
    {
        var property = propertyReference.Resolve();

        var propertyGetMethod = il.Import(property.GetMethod).MakeGeneric(updateCurrentMethod.GenericParameters.ToArray());

        // Emit: typeof(T), "PropertyName"
        il.EmitTypeof(type, getTypeFromHandleMethod)
          .Emit(OpCodes.Ldstr, property.Name);

        // Emit: ldftn(get_Property) — for virtual, wrap in a dispatcher that uses ldvirtftn+calli
        if (property.GetMethod.IsVirtual)
            propertyGetMethod = CreateDispatcher(assembly, propertyGetMethod);
        il.Emit(OpCodes.Ldftn, propertyGetMethod)
          // Set whether setter method uses a VirtualDispatch (static call) or instance call
          .Emit(property.GetMethod.IsVirtual ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

        // Emit: ldftn(set_Property) or IntPtr.Zero if no public setter
        if (property.SetMethod?.IsPublic == true)
        {
            var propertySetMethod = il.Import(property.SetMethod).MakeGeneric(updateCurrentMethod.GenericParameters.ToArray());
            if (property.SetMethod.IsVirtual)
                propertySetMethod = CreateDispatcher(assembly, propertySetMethod);
            il.Emit(OpCodes.Ldftn, propertySetMethod);
        }
        else
        {
            il.Emit(OpCodes.Ldc_I4_0)
              .Emit(OpCodes.Conv_I);
        }

        // Set whether setter method uses a VirtualDispatch (static call) or instance call
        il.Emit((property.SetMethod?.IsVirtual ?? false) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

        // Emit: new UpdatableProperty<PropType>(getter, isGetterVirtual, setter, isSetterVirtual)
        var propertyType = il.Import(replaceGenericsVisitor != null ? replaceGenericsVisitor.VisitDynamic(property.PropertyType) : property.PropertyType);
        var updatablePropertyInflatedCtor = GetOrCreateUpdatablePropertyCtor(assembly, propertyType);
        il.Emit(OpCodes.Newobj, updatablePropertyInflatedCtor)
          // Emit: UpdateEngine.RegisterMember(typeof(T), "PropertyName", updatableProperty);
          .Emit(OpCodes.Call, updateEngineRegisterMemberMethod);
    }

    private static string ComputeUpdateMethodName(TypeDefinition typeDefinition)
    {
        var typeName = ComputeTypeName(typeDefinition);

        return string.Format("UpdateGeneric_{0}", typeName);
    }

    private static string ComputeTypeName(TypeDefinition typeDefinition)
    {
        var typeName = typeDefinition.FullName.Replace(".", "_");
        var typeNameGenericPart = typeName.IndexOf("`");
        if (typeNameGenericPart != -1)
            typeName = typeName[..typeNameGenericPart];
        return typeName;
    }

    private static TypeDefinition GetOrCreateUpdateType(AssemblyDefinition assembly, bool createIfNotExists)
    {
        // Get or create module static constructor
        var updateEngineType = assembly.MainModule.Types.FirstOrDefault(x => x.Name == "UpdateEngineAutoGenerated");
        if (updateEngineType == null && createIfNotExists)
        {
            updateEngineType = new TypeDefinition(string.Empty, "UpdateEngineAutoGenerated", TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass)
            {
                BaseType = assembly.MainModule.TypeSystem.Object
            };
            assembly.MainModule.Types.Add(updateEngineType);
        }

        return updateEngineType;
    }

    public MethodReference GetOrCreateUpdatablePropertyCtor(AssemblyDefinition assembly, TypeReference propertyType)
    {
        // Use different type depending on if type is a struct or not
        var updatablePropertyGenericType =
            (propertyType.IsResolvedValueType() && (!propertyType.IsGenericInstance ||
                                                ((GenericInstanceType)propertyType).ElementType.FullName !=
                                                typeof(Nullable<>).FullName))
            ? updatablePropertyGenericCtor
            : updatablePropertyObjectGenericCtor;

        return assembly.MainModule.ImportReference(updatablePropertyGenericType).MakeGeneric(propertyType);
    }

    private static void EnumerateReferences(HashSet<AssemblyDefinition> assemblies, AssemblyDefinition assembly)
    {
        // Already processed?
        if (!assemblies.Add(assembly))
            return;

        // Let's recurse over referenced assemblies
        foreach (var referencedAssemblyName in assembly.MainModule.AssemblyReferences.ToArray())
        {
            // Avoid processing system assemblies
            // TODO: Scan what is actually in framework folders
            if (referencedAssemblyName.Name == "mscorlib" || referencedAssemblyName.Name.StartsWith("System")
                || referencedAssemblyName.FullName.Contains("PublicKeyToken=31bf3856ad364e35")) // Signed with Microsoft public key (likely part of system libraries)
            {
                continue;
            }

            try
            {
                var referencedAssembly = assembly.MainModule.AssemblyResolver.Resolve(referencedAssemblyName);

                EnumerateReferences(assemblies, referencedAssembly);
            }
            catch (AssemblyResolutionException)
            {
            }
        }
    }
}
