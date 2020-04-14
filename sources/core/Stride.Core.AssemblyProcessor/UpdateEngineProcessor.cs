// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Xenko.Core.AssemblyProcessor.Serializers;
using Xenko.Core.Serialization;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Xenko.Core.AssemblyProcessor
{
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

            // Make sure it is called at module startup
            var xenkoCoreModule = assembly.GetXenkoCoreModule();
            var moduleInitializerAttribute = xenkoCoreModule.GetType("Xenko.Core.ModuleInitializerAttribute");
            var ctorMethod = moduleInitializerAttribute.GetConstructors().Single(x => !x.IsStatic && !x.HasParameters);
            mainPrepareMethod.CustomAttributes.Add(new CustomAttribute(assembly.MainModule.ImportReference(ctorMethod)));

            return mainPrepareMethod;
        }

        private MethodReference CreateDispatcher(AssemblyDefinition assembly, MethodReference method)
        {
            var updateEngineType = GetOrCreateUpdateType(assembly, true);

            var dispatcherMethod = new MethodDefinition($"Dispatcher_{method.Name}", MethodAttributes.HideBySig | MethodAttributes.Assembly | MethodAttributes.Static, assembly.MainModule.ImportReference(method.ReturnType));
            updateEngineType.Methods.Add(dispatcherMethod);

            dispatcherMethod.Parameters.Add(new ParameterDefinition("this", Mono.Cecil.ParameterAttributes.None, method.DeclaringType));
            foreach (var param in method.Parameters)
            {
                dispatcherMethod.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
            }

            var il = dispatcherMethod.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            foreach (var param in dispatcherMethod.Parameters.Skip(1)) // first parameter is "this"
            {
                il.Emit(OpCodes.Ldarg, param);
            }
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldvirtftn, method);
            var callsite = new Mono.Cecil.CallSite(method.ReturnType) { HasThis = true };
            foreach (var param in method.Parameters)
                callsite.Parameters.Add(param);
            il.Emit(OpCodes.Calli, callsite);
            il.Emit(OpCodes.Ret);

            return dispatcherMethod;
        }

        public void ProcessSerializers(CecilSerializerContext context)
        {
            var references = new HashSet<AssemblyDefinition>();
            EnumerateReferences(references, context.Assembly);

            var coreAssembly = CecilExtensions.FindCorlibAssembly(context.Assembly);

            // Only process assemblies depending on Xenko.Engine
            if (!references.Any(x => x.Name.Name == "Xenko.Engine"))
            {
                // Make sure Xenko.Engine.Serializers can access everything internally
                var internalsVisibleToAttribute = coreAssembly.MainModule.GetTypeResolved(typeof(InternalsVisibleToAttribute).FullName);
                var serializationAssemblyName = "Xenko.Engine.Serializers";

                // Add [InteralsVisibleTo] attribute
                var internalsVisibleToAttributeCtor = context.Assembly.MainModule.ImportReference(internalsVisibleToAttribute.GetConstructors().Single());
                var internalsVisibleAttribute = new CustomAttribute(internalsVisibleToAttributeCtor)
                {
                    ConstructorArguments =
                            {
                                new CustomAttributeArgument(context.Assembly.MainModule.ImportReference(context.Assembly.MainModule.TypeSystem.String), serializationAssemblyName)
                            }
                };
                context.Assembly.CustomAttributes.Add(internalsVisibleAttribute);

                return;
            }



            var xenkoEngineAssembly = context.Assembly.Name.Name == "Xenko.Engine"
                    ? context.Assembly
                    : context.Assembly.MainModule.AssemblyResolver.Resolve(new AssemblyNameReference("Xenko.Engine", null));
            var xenkoEngineModule = xenkoEngineAssembly.MainModule;

            // Generate IL for Xenko.Core
            if (context.Assembly.Name.Name == "Xenko.Engine")
            {
                ProcessXenkoEngineAssembly(context);
            }

            var updatableFieldGenericType = xenkoEngineModule.GetType("Xenko.Updater.UpdatableField`1");
            updatableFieldGenericCtor = updatableFieldGenericType.Methods.First(x => x.IsConstructor && !x.IsStatic);

            updatablePropertyGenericType = xenkoEngineModule.GetType("Xenko.Updater.UpdatableProperty`1");
            updatablePropertyGenericCtor = updatablePropertyGenericType.Methods.First(x => x.IsConstructor && !x.IsStatic);

            var updatablePropertyObjectGenericType = xenkoEngineModule.GetType("Xenko.Updater.UpdatablePropertyObject`1");
            updatablePropertyObjectGenericCtor = updatablePropertyObjectGenericType.Methods.First(x => x.IsConstructor && !x.IsStatic);

            var updatableListUpdateResolverGenericType = xenkoEngineModule.GetType("Xenko.Updater.ListUpdateResolver`1");
            updatableListUpdateResolverGenericCtor = updatableListUpdateResolverGenericType.Methods.First(x => x.IsConstructor && !x.IsStatic);

            var updatableArrayUpdateResolverGenericType = xenkoEngineModule.GetType("Xenko.Updater.ArrayUpdateResolver`1");
            updatableArrayUpdateResolverGenericCtor = updatableArrayUpdateResolverGenericType.Methods.First(x => x.IsConstructor && !x.IsStatic);

            var parameterCollectionResolver = xenkoEngineModule.GetType("Xenko.Engine.Design.ParameterCollectionResolver");
            parameterCollectionResolverInstantiateValueAccessor = parameterCollectionResolver.Methods.First(x => x.Name == "InstantiateValueAccessor");

            var registerMemberMethod = xenkoEngineModule.GetType("Xenko.Updater.UpdateEngine").Methods.First(x => x.Name == "RegisterMember");
            updateEngineRegisterMemberMethod = context.Assembly.MainModule.ImportReference(registerMemberMethod);

            var registerMemberResolverMethod = xenkoEngineModule.GetType("Xenko.Updater.UpdateEngine") .Methods.First(x => x.Name == "RegisterMemberResolver");
            //pclVisitor.VisitMethod(registerMemberResolverMethod);
            updateEngineRegisterMemberResolverMethod = context.Assembly.MainModule.ImportReference(registerMemberResolverMethod);

            var typeType = coreAssembly.MainModule.GetTypeResolved(typeof(Type).FullName);
            getTypeFromHandleMethod = context.Assembly.MainModule.ImportReference(typeType.Methods.First(x => x.Name == "GetTypeFromHandle"));

            var mainPrepareMethod = CreateUpdateMethod(context.Assembly);

            // Emit serialization code for all the types we care about
            var processedTypes = new HashSet<TypeDefinition>(TypeReferenceEqualityComparer.Default);
            foreach (var serializableType in context.SerializableTypesProfiles.SelectMany(x => x.Value.SerializableTypes))
            {
                // Special case: when processing Xenko.Engine assembly, we automatically add dependent assemblies types too
                if (!serializableType.Value.Local && xenkoEngineAssembly != context.Assembly)
                    continue;

                var typeDefinition = serializableType.Key as TypeDefinition;
                if (typeDefinition == null)
                    continue;

                // Ignore already processed types
                if (!processedTypes.Add(typeDefinition))
                    continue;

                try
                {
                    ProcessType(context, context.Assembly.MainModule.ImportReference(typeDefinition), mainPrepareMethod);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(string.Format("Error when generating update engine code for {0}", typeDefinition), e);
                }
            }

            // Force generic instantiations
            var il = mainPrepareMethod.Body.GetILProcessor();
            foreach (var serializableType in context.SerializableTypesProfiles.SelectMany(x => x.Value.SerializableTypes).ToArray())
            {
                // Special case: when processing Xenko.Engine assembly, we automatically add dependent assemblies types too
                if (!serializableType.Value.Local && xenkoEngineAssembly != context.Assembly)
                    continue;

                // Try to find if original method definition was generated  
                var typeDefinition = serializableType.Key.Resolve();

                // If using List<T>, register this type in UpdateEngine
                var parentTypeDefinition = typeDefinition;
                while(parentTypeDefinition != null)
                {
                    var listInterfaceType = parentTypeDefinition.Interfaces.Select(x => x.InterfaceType).OfType<GenericInstanceType>().FirstOrDefault(x => x.ElementType.FullName == typeof(IList<>).FullName);
                    if (listInterfaceType != null)
                    {
                        //call Updater.UpdateEngine.RegisterMemberResolver(new Updater.ListUpdateResolver<T>());
                        var elementType = ResolveGenericsVisitor.Process(serializableType.Key, listInterfaceType.GenericArguments[0]);
                        il.Emit(OpCodes.Newobj, context.Assembly.MainModule.ImportReference(updatableListUpdateResolverGenericCtor).MakeGeneric(context.Assembly.MainModule.ImportReference(elementType)));
                        il.Emit(OpCodes.Call, updateEngineRegisterMemberResolverMethod);
                    }

                    parentTypeDefinition = parentTypeDefinition.BaseType?.Resolve();
                }

                // Same for arrays
                var arrayType = serializableType.Key as ArrayType;
                if (arrayType != null)
                {
                    //call Updater.UpdateEngine.RegisterMemberResolver(new Updater.ArrayUpdateResolver<T>());
                    var elementType = ResolveGenericsVisitor.Process(serializableType.Key, arrayType.ElementType);
                    il.Emit(OpCodes.Newobj, context.Assembly.MainModule.ImportReference(updatableArrayUpdateResolverGenericCtor).MakeGeneric(context.Assembly.MainModule.ImportReference(elementType)));
                    il.Emit(OpCodes.Call, updateEngineRegisterMemberResolverMethod);
                }

                // Generic instantiation for AOT platforms
                if (context.Platform == Core.PlatformType.iOS && serializableType.Key.Name == "ValueParameterKey`1")
                {
                    var keyType = ((GenericInstanceType)serializableType.Key).GenericArguments[0];
                    il.Emit(OpCodes.Call, context.Assembly.MainModule.ImportReference(parameterCollectionResolverInstantiateValueAccessor).MakeGenericMethod(context.Assembly.MainModule.ImportReference(keyType)));
                }

                var genericInstanceType = serializableType.Key as GenericInstanceType;
                if (genericInstanceType != null)
                {
                    var expectedUpdateMethodName = ComputeUpdateMethodName(typeDefinition);
                    var updateMethod = GetOrCreateUpdateType(typeDefinition.Module.Assembly, false)?.Methods.FirstOrDefault(x => x.Name == expectedUpdateMethodName && x.HasGenericParameters && x.GenericParameters.Count == genericInstanceType.GenericParameters.Count);

                    // If nothing was found in main assembly, also look in Xenko.Engine assembly, just in case (it might defines some shared/corlib types -- currently not the case)
                    if (updateMethod == null)
                    {
                        updateMethod = GetOrCreateUpdateType(xenkoEngineAssembly, false)?.Methods.FirstOrDefault(x => x.Name == expectedUpdateMethodName && x.HasGenericParameters && x.GenericParameters.Count == genericInstanceType.GenericParameters.Count);
                    }

                    if (updateMethod != null)
                    {
                        // Emit call to update engine setup method with generic arguments of current type
                        il.Emit(OpCodes.Call, context.Assembly.MainModule.ImportReference(updateMethod)
                            .MakeGenericMethod(genericInstanceType.GenericArguments
                                .Select(context.Assembly.MainModule.ImportReference)
                                .ToArray()));
                    }
                }
            }

            il.Emit(OpCodes.Ret);
        }

        public void ProcessType(CecilSerializerContext context, TypeReference type, MethodDefinition updateMainMethod)
        {
            var typeDefinition = type.Resolve();

            // No need to process enum
            if (typeDefinition.IsEnum)
                return;

            var updateCurrentMethod = updateMainMethod;
            ResolveGenericsVisitor replaceGenericsVisitor = null;

            if (typeDefinition.HasGenericParameters)
            {
                // Make a prepare method for just this object since it might need multiple instantiation
                updateCurrentMethod = new MethodDefinition(ComputeUpdateMethodName(typeDefinition), MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static, context.Assembly.MainModule.TypeSystem.Void);
                var genericsMapping = new Dictionary<TypeReference, TypeReference>();
                foreach (var genericParameter in typeDefinition.GenericParameters)
                {
                    var genericParameterCopy = new GenericParameter(genericParameter.Name, updateCurrentMethod)
                    {
                        Attributes = genericParameter.Attributes,
                    };
                    foreach (var constraint in genericParameter.Constraints)
                        genericParameterCopy.Constraints.Add(context.Assembly.MainModule.ImportReference(constraint));
                    updateCurrentMethod.GenericParameters.Add(genericParameterCopy);

                    genericsMapping[genericParameter] = genericParameterCopy;
                }

                replaceGenericsVisitor = new ResolveGenericsVisitor(genericsMapping);

                updateMainMethod.DeclaringType.Methods.Add(updateCurrentMethod);
            }

            var il = updateCurrentMethod.Body.GetILProcessor();
            var typeIsValueType = type.IsResolvedValueType();
            var emptyObjectField = typeIsValueType ? null : updateMainMethod.DeclaringType.Fields.FirstOrDefault(x => x.Name == "emptyObject");
            VariableDefinition emptyStruct = null;

            // Note: forcing fields and properties to be processed in all cases
            foreach (var serializableItem in ComplexSerializerRegistry.GetSerializableItems(type, true, ComplexTypeSerializerFlags.SerializePublicFields | ComplexTypeSerializerFlags.SerializePublicProperties | ComplexTypeSerializerFlags.Updatable))
            {
                if (serializableItem.MemberInfo is FieldReference fieldReference)
                {
                    var field = fieldReference.Resolve();

                    // First time it is needed, let's create empty object in the class (var emptyObject = new object()) or empty local struct in the method
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
                            emptyObjectField = new FieldDefinition("emptyObject", FieldAttributes.Static | FieldAttributes.Private, context.Assembly.MainModule.TypeSystem.Object);

                            // Create static ctor that will initialize this object
                            var staticConstructor = new MethodDefinition(".cctor",
                                                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                                                    context.Assembly.MainModule.TypeSystem.Void);
                            var staticConstructorIL = staticConstructor.Body.GetILProcessor();
                            staticConstructorIL.Emit(OpCodes.Newobj, context.Assembly.MainModule.ImportReference(emptyObjectField.FieldType.Resolve().GetConstructors().Single(x => !x.IsStatic && !x.HasParameters)));
                            staticConstructorIL.Emit(OpCodes.Stsfld, emptyObjectField);
                            staticConstructorIL.Emit(OpCodes.Ret);

                            updateMainMethod.DeclaringType.Fields.Add(emptyObjectField);
                            updateMainMethod.DeclaringType.Methods.Add(staticConstructor);
                        }
                    }


                    il.Emit(OpCodes.Ldtoken, type);
                    il.Emit(OpCodes.Call, getTypeFromHandleMethod);
                    il.Emit(OpCodes.Ldstr, field.Name);

                    if (typeIsValueType)
                        il.Emit(OpCodes.Ldloca, emptyStruct);
                    else
                        il.Emit(OpCodes.Ldsfld, emptyObjectField);
                    il.Emit(OpCodes.Ldflda, context.Assembly.MainModule.ImportReference(fieldReference));
                    il.Emit(OpCodes.Conv_I);
                    if (typeIsValueType)
                        il.Emit(OpCodes.Ldloca, emptyStruct);
                    else
                        il.Emit(OpCodes.Ldsfld, emptyObjectField);
                    il.Emit(OpCodes.Conv_I);
                    il.Emit(OpCodes.Sub);
                    il.Emit(OpCodes.Conv_I4);

                    var fieldType = context.Assembly.MainModule.ImportReference(replaceGenericsVisitor != null ? replaceGenericsVisitor.VisitDynamic(field.FieldType) : field.FieldType);
                    il.Emit(OpCodes.Newobj, context.Assembly.MainModule.ImportReference(updatableFieldGenericCtor).MakeGeneric(fieldType));
                    il.Emit(OpCodes.Call, updateEngineRegisterMemberMethod);
                }

                var propertyReference = serializableItem.MemberInfo as PropertyReference;
                if (propertyReference != null)
                {
                    var property = propertyReference.Resolve();

                    var propertyGetMethod = context.Assembly.MainModule.ImportReference(property.GetMethod).MakeGeneric(updateCurrentMethod.GenericParameters.ToArray());

                    il.Emit(OpCodes.Ldtoken, type);
                    il.Emit(OpCodes.Call, getTypeFromHandleMethod);
                    il.Emit(OpCodes.Ldstr, property.Name);

                    // If it's a virtual or interface call, we need to create a dispatcher using ldvirtftn
                    if (property.GetMethod.IsVirtual)
                        propertyGetMethod = CreateDispatcher(context.Assembly, propertyGetMethod);

                    il.Emit(OpCodes.Ldftn, propertyGetMethod);

                    // Set whether getter method uses a VirtualDispatch (static call) or instance call
                    il.Emit(property.GetMethod.IsVirtual ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

                    // Only uses setter if it exists and it's public
                    if (property.SetMethod != null && property.SetMethod.IsPublic)
                    {
                        var propertySetMethod = context.Assembly.MainModule.ImportReference(property.SetMethod).MakeGeneric(updateCurrentMethod.GenericParameters.ToArray());
                        if (property.SetMethod.IsVirtual)
                            propertySetMethod = CreateDispatcher(context.Assembly, propertySetMethod);
                        il.Emit(OpCodes.Ldftn, propertySetMethod);
                    }
                    else
                    {
                        // 0 (native int)
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Conv_I);
                    }

                    // Set whether setter method uses a VirtualDispatch (static call) or instance call
                    il.Emit((property.SetMethod?.IsVirtual ?? false) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

                    var propertyType = context.Assembly.MainModule.ImportReference(replaceGenericsVisitor != null ? replaceGenericsVisitor.VisitDynamic(property.PropertyType) : property.PropertyType);

                    var updatablePropertyInflatedCtor = GetOrCreateUpdatablePropertyCtor(context.Assembly, propertyType);

                    il.Emit(OpCodes.Newobj, updatablePropertyInflatedCtor);
                    il.Emit(OpCodes.Call, updateEngineRegisterMemberMethod);
                }
            }

            if (updateCurrentMethod != updateMainMethod)
            {
                // If we have a local method, close it
                il.Emit(OpCodes.Ret);

                // Also call it from main method if it was a closed generic instantiation
                if (type is GenericInstanceType)
                {
                    il = updateMainMethod.Body.GetILProcessor();
                    il.Emit(OpCodes.Call, updateCurrentMethod.MakeGeneric(((GenericInstanceType)type).GenericArguments.Select(context.Assembly.MainModule.ImportReference).ToArray()));
                }
            }
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
                typeName = typeName.Substring(0, typeNameGenericPart);
            return typeName;
        }

        private static TypeDefinition GetOrCreateUpdateType(AssemblyDefinition assembly, bool createIfNotExists)
        {
            // Get or create module static constructor
            var updateEngineType = assembly.MainModule.Types.FirstOrDefault(x => x.Name == "UpdateEngineAutoGenerated");
            if (updateEngineType == null && createIfNotExists)
            {
                updateEngineType = new TypeDefinition(string.Empty, "UpdateEngineAutoGenerated", TypeAttributes.Public | TypeAttributes.AutoClass | TypeAttributes.AnsiClass);
                updateEngineType.BaseType = assembly.MainModule.TypeSystem.Object;
                assembly.MainModule.Types.Add(updateEngineType);
            }

            return updateEngineType;
        }

        public MethodReference GetOrCreateUpdatablePropertyCtor(AssemblyDefinition assembly, TypeReference propertyType)
        {
            // Use different type depending on if type is a struct or not
            var updatablePropertyGenericType =
                (propertyType.IsResolvedValueType() && (!propertyType.IsGenericInstance ||
                                                    ((GenericInstanceType) propertyType).ElementType.FullName !=
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
                    continue;

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
};
