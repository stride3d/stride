// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Stride.Core.Serialization;
using Stride.Core.Storage;
using CustomAttributeNamedArgument = Mono.Cecil.CustomAttributeNamedArgument;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Stride.Core.AssemblyProcessor
{
    internal class SerializationProcessor : IAssemblyDefinitionProcessor
    {
        public delegate void RegisterSourceCode(string code, string name = null);

        public SerializationProcessor()
        {
        }

        public bool Process(AssemblyProcessorContext context)
        {
            var registry = new ComplexSerializerRegistry(context.Platform, context.Assembly, context.Log);

            // Register default serialization profile (to help AOT generic instantiation of serializers)
            RegisterDefaultSerializationProfile(context.AssemblyResolver, context.Assembly, registry, context.Log);

            // Generate serializer code
            // Create the serializer code generator
            //var serializerGenerator = new ComplexSerializerCodeGenerator(registry);
            //sourceCodeRegisterAction(serializerGenerator.TransformText(), "DataSerializers");

            GenerateSerializerCode(registry, out var serializationHash);

            context.SerializationHash = serializationHash;

            return true;
        }

        /// <summary>
        /// Generates serializer code using Cecil.
        /// Note: we might want something more fluent? (probably lot of work to get the system working, but would make changes easier to do -- not sure if worth it considering it didn't change much recently)
        /// </summary>
        /// <param name="registry"></param>
        private static void GenerateSerializerCode(ComplexSerializerRegistry registry, out ObjectId serializationHash)
        {
            var hash = new ObjectIdBuilder();

            // First, hash global binary format version, in case it gets bumped
            hash.Write(DataSerializer.BinaryFormatVersion);

            var assembly = registry.Assembly;
            var strideCoreModule = assembly.GetStrideCoreModule();

            var dataSerializerTypeRef = assembly.MainModule.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.DataSerializer`1"));
            var serializerSelectorType = strideCoreModule.GetType("Stride.Core.Serialization.SerializerSelector");
            var serializerSelectorTypeRef = assembly.MainModule.ImportReference(serializerSelectorType);
            var serializerSelectorGetSerializerRef = assembly.MainModule.ImportReference(serializerSelectorType.Methods.Single(x => x.Name == "GetSerializer" && x.Parameters.Count == 0 && x.GenericParameters.Count == 1));
            var memberSerializerCreateRef = assembly.MainModule.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.MemberSerializer`1").Methods.Single(x => x.Name == "Create"));

            var dataSerializerSerializeMethod = dataSerializerTypeRef.Resolve().Methods.Single(x => x.Name == "Serialize" && (x.Attributes & MethodAttributes.Abstract) != 0);
            var dataSerializerSerializeMethodRef = assembly.MainModule.ImportReference(dataSerializerSerializeMethod);

            // Generate serializer code for each type (we generate code similar to ComplexClassSerializerGenerator.tt, see this file for reference)
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
                if (complexType.Value.ComplexSerializerProcessParentType)
                {
                    parentType = ResolveGenericsVisitor.Process(serializerType, type.BaseType);
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
                    MethodAttributes.Public, assembly.MainModule.TypeSystem.Void);
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, assembly.MainModule.ImportReference(serializerType.BaseType.Resolve().GetEmptyConstructor(true)).MakeGeneric(typeWithGenerics)));
                ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                serializerType.Methods.Add(ctor);

                // Add Initialize method
                var initialize = new MethodDefinition("Initialize", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, assembly.MainModule.TypeSystem.Void);
                initialize.Parameters.Add(new ParameterDefinition("serializerSelector", ParameterAttributes.None, serializerSelectorTypeRef));
                if (complexType.Value.ComplexSerializerProcessParentType)
                {
                    initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                    initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, serializerSelectorGetSerializerRef.MakeGenericMethod(parentType)));
                    initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, parentSerializerField.MakeGeneric(genericParameters)));
                }
                foreach (var serializableItem in serializableItemInfos)
                {
                    initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                    initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                    initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Call, memberSerializerCreateRef.MakeGeneric(serializableItem.Value.Type)));
                    initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, serializableItem.Value.SerializerField.MakeGeneric(genericParameters)));
                }
                initialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                serializerType.Methods.Add(initialize);

                // Add Serialize method
                var serialize = new MethodDefinition("Serialize", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, assembly.MainModule.TypeSystem.Void);
                serialize.Parameters.Add(new ParameterDefinition("obj", ParameterAttributes.None, typeWithGenerics.MakeByReferenceType()));
                // Copy other parameters from parent method
                for (int i = 1; i < dataSerializerSerializeMethod.Parameters.Count; ++i)
                {
                    var parentParameter = dataSerializerSerializeMethod.Parameters[i];
                    serialize.Parameters.Add(new ParameterDefinition(parentParameter.Name, ParameterAttributes.None, assembly.MainModule.ImportReference(parentParameter.ParameterType)));
                }

                if (complexType.Value.ComplexSerializerProcessParentType)
                {
                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, parentSerializerField.MakeGeneric(genericParameters)));
                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, dataSerializerSerializeMethodRef.MakeGeneric(parentType)));
                }

                if (serializableItems.Length > 0)
                {
                    var blockStartInstructions = new[] { Instruction.Create(OpCodes.Nop), Instruction.Create(OpCodes.Nop) };
                    // Iterate over ArchiveMode
                    for (int i = 0; i < 2; ++i)
                    {
                        var archiveMode = i == 0 ? ArchiveMode.Serialize : ArchiveMode.Deserialize;

                        // Check mode
                        if (archiveMode == ArchiveMode.Serialize)
                        {
                            serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
                            serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, (int) archiveMode));
                            serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
                            serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, blockStartInstructions[0]));
                        }
                        else
                        {
                            serialize.Body.Instructions.Add(blockStartInstructions[0]);
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
                            serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                            serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, serializableItemInfo.SerializerField.MakeGeneric(genericParameters)));

                            var fieldReference = serializableItem.MemberInfo is FieldReference ? assembly.MainModule.ImportReference((FieldReference)serializableItem.MemberInfo).MakeGeneric(genericParameters) : null;

                            if (memberVariableName != null)
                            {
                                // Use a temporary variable
                                if (!localsByTypes.TryGetValue(serializableItemInfo.Type, out var tempLocal))
                                {
                                    tempLocal = new VariableDefinition(serializableItemInfo.Type);
                                    localsByTypes.Add(serializableItemInfo.Type, tempLocal);
                                    serialize.Body.Variables.Add(tempLocal);
                                    serialize.Body.InitLocals = true;
                                }

                                if (!(archiveMode == ArchiveMode.Deserialize && memberAssignBack))
                                {
                                    // obj.Member
                                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                                    if (!type.IsValueType)
                                        serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldind_Ref));

                                    if (serializableItem.MemberInfo is PropertyDefinition property)
                                    {
                                        var getMethod = property.Resolve().GetMethod;
                                        serialize.Body.Instructions.Add(Instruction.Create(getMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, assembly.MainModule.ImportReference(getMethod).MakeGeneric(genericParameters)));
                                    }
                                    else if (serializableItem.MemberInfo is FieldDefinition)
                                    {
                                        serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldReference));
                                    }
                                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, tempLocal));
                                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, tempLocal));
                                }
                                else
                                {
                                    // default(T)
                                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloca, tempLocal));
                                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
                                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Initobj, serializableItemInfo.Type));
                                }
                            }
                            else
                            {
                                // Use object directly
                                serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                                if (!type.IsValueType)
                                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldind_Ref));
                                serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldflda, fieldReference));
                            }
                            serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
                            serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));

                            serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, dataSerializerSerializeMethodRef.MakeGeneric(serializableItemInfo.Type)));

                            if (archiveMode == ArchiveMode.Deserialize && memberVariableName != null && memberAssignBack)
                            {
                                // Need to copy back to object
                                serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                                if (!type.IsValueType)
                                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldind_Ref));

                                serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, localsByTypes[serializableItemInfo.Type]));

                                if (serializableItem.MemberInfo is PropertyDefinition property)
                                {
                                    var setMethod = property.Resolve().SetMethod;
                                    serialize.Body.Instructions.Add(Instruction.Create(setMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, assembly.MainModule.ImportReference(setMethod).MakeGeneric(genericParameters)));
                                }
                                else if (serializableItem.MemberInfo is FieldDefinition)
                                {
                                    serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, fieldReference));
                                }
                            }
                        }

                        if (archiveMode == ArchiveMode.Serialize)
                        {
                            serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Br, blockStartInstructions[1]));
                        }
                    }

                    serialize.Body.Instructions.Add(blockStartInstructions[1]);
                }
                serialize.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                serializerType.Methods.Add(serialize);

                //assembly.MainModule.Types.Add(serializerType);
            }

            var mscorlibAssembly = CecilExtensions.FindCorlibAssembly(assembly);
            var reflectionAssembly = CecilExtensions.FindReflectionAssembly(assembly);

            // String
            var stringType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(string).FullName);
            var stringTypeRef = assembly.MainModule.ImportReference(stringType);
            // Type
            var typeType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(Type).FullName);
            var typeTypeRef = assembly.MainModule.ImportReference(typeType);
            var getTypeFromHandleMethod = typeType.Methods.First(x => x.Name == nameof(Type.GetTypeFromHandle));
            var getTokenInfoExMethod = reflectionAssembly.MainModule.GetTypeResolved("System.Reflection.IntrospectionExtensions").Resolve().Methods.First(x => x.Name == nameof(IntrospectionExtensions.GetTypeInfo));
            var typeInfoType = reflectionAssembly.MainModule.GetTypeResolved(typeof(TypeInfo).FullName);
            // Note: TypeInfo.Assembly/Module could be on the type itself or on its parent MemberInfo depending on runtime
            var getTypeInfoAssembly = typeInfoType.Properties.Concat(typeInfoType.BaseType.Resolve().Properties).First(x => x.Name == nameof(TypeInfo.Assembly)).GetMethod;
            var getTypeInfoModule = typeInfoType.Properties.Concat(typeInfoType.BaseType.Resolve().Properties).First(x => x.Name == nameof(TypeInfo.Module)).GetMethod;
            var typeHandleProperty = typeType.Properties.First(x => x.Name == nameof(Type.TypeHandle));
            var getTypeHandleMethodRef = assembly.MainModule.ImportReference(typeHandleProperty.GetMethod);

            // Generate code
            var serializerFactoryType = new TypeDefinition("Stride.Core.DataSerializers",
                Utilities.BuildValidClassName(assembly.Name.Name) + "SerializerFactory",
                TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract,
                assembly.MainModule.TypeSystem.Object);
            assembly.MainModule.Types.Add(serializerFactoryType);

            var dataSerializerModeTypeRef = assembly.MainModule.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.DataSerializerGenericMode"));

            var dataSerializerGlobalAttribute = strideCoreModule.GetType("Stride.Core.Serialization.DataSerializerGlobalAttribute");
            var dataSerializerGlobalCtorRef = assembly.MainModule.ImportReference(dataSerializerGlobalAttribute.GetConstructors().Single(x => !x.IsStatic && x.Parameters.Count == 5));

            foreach (var profile in registry.Context.SerializableTypesProfiles)
            {
                foreach (var type in profile.Value.SerializableTypes.Where(x => x.Value.Local))
                {
                    // Generating: [DataSerializerGlobalAttribute(<#= type.Value.SerializerType != null ? $"typeof({type.Value.SerializerType.ConvertCSharp(false)})" : "null" #>, typeof(<#= type.Key.ConvertCSharp(false) #>), DataSerializerGenericMode.<#= type.Value.Mode.ToString() #>, <#=type.Value.Inherited ? "true" : "false"#>, <#=type.Value.ComplexSerializer ? "true" : "false"#>, Profile = "<#=profile.Key#>")]
                    serializerFactoryType.CustomAttributes.Add(new CustomAttribute(dataSerializerGlobalCtorRef)
                    {
                        ConstructorArguments =
                        {
                            new CustomAttributeArgument(typeTypeRef, type.Value.SerializerType != null ? assembly.MainModule.ImportReference(type.Value.SerializerType) : null),
                            new CustomAttributeArgument(typeTypeRef, assembly.MainModule.ImportReference(type.Key)),
                            new CustomAttributeArgument(dataSerializerModeTypeRef, type.Value.Mode),
                            new CustomAttributeArgument(assembly.MainModule.TypeSystem.Boolean, type.Value.Inherited),
                            new CustomAttributeArgument(assembly.MainModule.TypeSystem.Boolean, type.Value.ComplexSerializer),
                        },
                        Properties =
                        {
                            new CustomAttributeNamedArgument("Profile", new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, profile.Key))
                        },
                    });
                }
                foreach (var type in profile.Value.GenericSerializableTypes.Where(x => x.Value.Local))
                {
                    // Generating: [DataSerializerGlobalAttribute(<#= type.Value.SerializerType != null ? $"typeof({type.Value.SerializerType.ConvertCSharp(true)})" : "null" #>, typeof(<#= type.Key.ConvertCSharp(true) #>), DataSerializerGenericMode.<#= type.Value.Mode.ToString() #>, <#=type.Value.Inherited ? "true" : "false"#>, <#=type.Value.ComplexSerializer ? "true" : "false"#>, Profile = "<#=profile.Key#>")]
                    serializerFactoryType.CustomAttributes.Add(new CustomAttribute(dataSerializerGlobalCtorRef)
                    {
                        ConstructorArguments =
                        {
                            new CustomAttributeArgument(typeTypeRef, type.Value.SerializerType != null ? assembly.MainModule.ImportReference(type.Value.SerializerType) : null),
                            new CustomAttributeArgument(typeTypeRef, assembly.MainModule.ImportReference(type.Key)),
                            new CustomAttributeArgument(dataSerializerModeTypeRef, type.Value.Mode),
                            new CustomAttributeArgument(assembly.MainModule.TypeSystem.Boolean, type.Value.Inherited),
                            new CustomAttributeArgument(assembly.MainModule.TypeSystem.Boolean, type.Value.ComplexSerializer),
                        },
                        Properties =
                        {
                            new CustomAttributeNamedArgument("Profile", new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, profile.Key))
                        },
                    });
                }
            }

            // Create Initialize method
            var initializeMethod = new MethodDefinition("Initialize",
                MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Static,
                assembly.MainModule.TypeSystem.Void);
            serializerFactoryType.Methods.Add(initializeMethod);

            // Make sure it is called at module startup
            initializeMethod.AddModuleInitializer(-1000);

            var initializeMethodIL = initializeMethod.Body.GetILProcessor();

            // Generating: var assemblySerializers = new AssemblySerializers(typeof(<#=registry.ClassName#>).GetTypeInfo().Assembly);
            var assemblySerializersType = strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializers");

            var assemblySerializersGetDataContractAliasesRef = assembly.MainModule.ImportReference(assemblySerializersType.Properties.First(x => x.Name == "DataContractAliases").GetMethod);
            var assemblySerializersGetDataContractAliasesAdd = assemblySerializersGetDataContractAliasesRef.ReturnType.Resolve().Methods.First(x => x.Name == "Add");
            var dataContractAliasTypeRef = ((GenericInstanceType)assemblySerializersGetDataContractAliasesRef.ReturnType).GenericArguments[0];
            var dataContractAliasTypeCtorRef = assembly.MainModule.ImportReference(dataContractAliasTypeRef.Resolve().GetConstructors().Single());
            var assemblySerializersGetDataContractAliasesAddRef = assembly.MainModule.ImportReference(assemblySerializersGetDataContractAliasesAdd).MakeGeneric(dataContractAliasTypeRef);
            initializeMethodIL.Emit(OpCodes.Ldtoken, serializerFactoryType);
            initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod));
            initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTokenInfoExMethod));
            initializeMethodIL.Emit(OpCodes.Callvirt, assembly.MainModule.ImportReference(getTypeInfoAssembly));
            initializeMethodIL.Emit(OpCodes.Newobj, assembly.MainModule.ImportReference(assemblySerializersType.Methods.Single(x => x.IsConstructor && x.Parameters.Count == 1)));

            foreach (var alias in registry.Context.DataContractAliases)
            {
                initializeMethodIL.Emit(OpCodes.Dup);

                // Generating: assemblySerializers.DataContractAliases.Add(new AssemblySerializers.DataContractAlias(@"<#= alias.Item1 #>", typeof(<#= alias.Item2.ConvertCSharp(true) #>), <#=alias.Item3 ? "true" : "false"#>));
                initializeMethodIL.Emit(OpCodes.Call, assemblySerializersGetDataContractAliasesRef);
                initializeMethodIL.Emit(OpCodes.Ldstr, alias.Item1);
                initializeMethodIL.Emit(OpCodes.Ldtoken, assembly.MainModule.ImportReference(alias.Item2));
                initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod));
                initializeMethodIL.Emit(alias.Item3 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                initializeMethodIL.Emit(OpCodes.Newobj, dataContractAliasTypeCtorRef);
                initializeMethodIL.Emit(OpCodes.Call, assemblySerializersGetDataContractAliasesAddRef);
            }

            var assemblySerializersGetModulesRef = assembly.MainModule.ImportReference(assemblySerializersType.Properties.First(x => x.Name == "Modules").GetMethod);
            var assemblySerializersGetModulesAdd = assemblySerializersGetModulesRef.ReturnType.Resolve().Methods.First(x => x.Name == "Add");
            var moduleRef = ((GenericInstanceType)assemblySerializersGetModulesRef.ReturnType).GenericArguments[0];
            var assemblySerializersGetModulesAddRef = assembly.MainModule.ImportReference(assemblySerializersGetModulesAdd).MakeGeneric(moduleRef);

            foreach (var referencedAssemblySerializerFactoryType in registry.ReferencedAssemblySerializerFactoryTypes)
            {
                initializeMethodIL.Emit(OpCodes.Dup);

                // Generating: assemblySerializers.Modules.Add(typeof(<#=referencedAssemblySerializerFactoryType.ConvertCSharp()#>).GetTypeInfo().Module);
                initializeMethodIL.Emit(OpCodes.Call, assemblySerializersGetModulesRef);
                initializeMethodIL.Emit(OpCodes.Ldtoken, assembly.MainModule.ImportReference(referencedAssemblySerializerFactoryType));
                initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod));
                initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTokenInfoExMethod));
                initializeMethodIL.Emit(OpCodes.Callvirt, assembly.MainModule.ImportReference(getTypeInfoModule));
                initializeMethodIL.Emit(OpCodes.Call, assemblySerializersGetModulesAddRef);
            }

            var objectIdCtorRef = assembly.MainModule.ImportReference(strideCoreModule.GetType("Stride.Core.Storage.ObjectId").GetConstructors().Single(x => x.Parameters.Count == 4));
            var serializerEntryTypeCtorRef = assembly.MainModule.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializerEntry").GetConstructors().Single());
            var assemblySerializersPerProfileType = strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializersPerProfile");
            var assemblySerializersPerProfileTypeAddRef = assembly.MainModule.ImportReference(assemblySerializersPerProfileType.BaseType.Resolve().Methods.First(x => x.Name == "Add")).MakeGeneric(serializerEntryTypeCtorRef.DeclaringType);
            var assemblySerializersPerProfileTypeCtorRef = assembly.MainModule.ImportReference(assemblySerializersPerProfileType.GetEmptyConstructor());
            var assemblySerializersGetProfilesRef = assembly.MainModule.ImportReference(assemblySerializersType.Properties.First(x => x.Name == "Profiles").GetMethod);
            var assemblySerializersGetProfilesSetItemRef = assembly.MainModule.ImportReference(assemblySerializersGetProfilesRef.ReturnType.Resolve().Methods.First(x => x.Name == "set_Item"))
                .MakeGeneric(((GenericInstanceType)assemblySerializersGetProfilesRef.ReturnType).GenericArguments.ToArray());

            var runtimeHelpersType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(RuntimeHelpers).FullName);
            var runClassConstructorMethod = assembly.MainModule.ImportReference(runtimeHelpersType.Methods.Single(x => x.IsPublic && x.Name == "RunClassConstructor" && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.FullName == typeof(RuntimeTypeHandle).FullName));

            foreach (var profile in registry.Context.SerializableTypesProfiles)
            {
                initializeMethodIL.Emit(OpCodes.Dup);

                // Generating: var assemblySerializersProfile = new AssemblySerializersPerProfile();
                // Generating: assemblySerializers.Profiles["<#=profile.Key#>"] = assemblySerializersProfile;
                initializeMethodIL.Emit(OpCodes.Callvirt, assemblySerializersGetProfilesRef);
                initializeMethodIL.Emit(OpCodes.Ldstr, profile.Key);
                initializeMethodIL.Emit(OpCodes.Newobj, assemblySerializersPerProfileTypeCtorRef);

                foreach (var type in profile.Value.SerializableTypes.Where(x => x.Value.Local))
                {
                    // Generating: assemblySerializersProfile.Add(new AssemblySerializerEntry(<#=type.Key.ConvertTypeId()#>, typeof(<#= type.Key.ConvertCSharp() #>), <# if (type.Value.SerializerType != null) { #>typeof(<#= type.Value.SerializerType.ConvertCSharp() #>)<# } else { #>null<# } #>));
                    initializeMethodIL.Emit(OpCodes.Dup);

                    var typeName = type.Key.ConvertCSharp(false);
                    var typeId = ObjectId.FromBytes(Encoding.UTF8.GetBytes(typeName));

                    unsafe
                    {
                            var typeIdHash = (int*)&typeId;

                            for (int i = 0; i < ObjectId.HashSize / 4; ++i)
                                initializeMethodIL.Emit(OpCodes.Ldc_I4, typeIdHash[i]);
                        }

                    initializeMethodIL.Emit(OpCodes.Newobj, objectIdCtorRef);

                    initializeMethodIL.Emit(OpCodes.Ldtoken, assembly.MainModule.ImportReference(type.Key));
                    initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod));

                    if (type.Value.SerializerType != null)
                    {
                        initializeMethodIL.Emit(OpCodes.Ldtoken, assembly.MainModule.ImportReference(type.Value.SerializerType));
                        initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod));
                    }
                    else
                    {
                        initializeMethodIL.Emit(OpCodes.Ldnull);
                    }

                    initializeMethodIL.Emit(OpCodes.Newobj, serializerEntryTypeCtorRef);
                    initializeMethodIL.Emit(OpCodes.Callvirt, assemblySerializersPerProfileTypeAddRef);

                    if (type.Value.SerializerType?.Resolve()?.Methods.Any(x => x.IsConstructor && x.IsStatic) == true)
                    {
                        // Generating: System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(<#=type.Value.SerializerType.ConvertCSharp()#>).TypeHandle);
                        initializeMethodIL.Append(Instruction.Create(OpCodes.Ldtoken, type.Value.SerializerType));
                        initializeMethodIL.Append(Instruction.Create(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod)));
                        initializeMethodIL.Append(Instruction.Create(OpCodes.Callvirt, getTypeHandleMethodRef));
                        initializeMethodIL.Append(Instruction.Create(OpCodes.Call, runClassConstructorMethod));
                    }
                }

                initializeMethodIL.Emit(OpCodes.Callvirt, assemblySerializersGetProfilesSetItemRef);
            }

            // Generating: DataSerializerFactory.RegisterSerializationAssembly(assemblySerializers);
            var dataSerializerFactoryRegisterSerializationAssemblyMethodRef = assembly.MainModule.ImportReference(strideCoreModule.GetType("Stride.Core.Serialization.DataSerializerFactory").Methods.Single(x => x.Name == "RegisterSerializationAssembly" && x.Parameters[0].ParameterType.FullName == assemblySerializersType.FullName));
            initializeMethodIL.Emit(OpCodes.Call, dataSerializerFactoryRegisterSerializationAssemblyMethodRef);

            // Generating: AssemblyRegistry.Register(typeof(<#=registry.ClassName#>).GetTypeInfo().Assembly, AssemblyCommonCategories.Engine);
            initializeMethodIL.Emit(OpCodes.Ldtoken, serializerFactoryType);
            initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod));
            initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTokenInfoExMethod));
            initializeMethodIL.Emit(OpCodes.Callvirt, assembly.MainModule.ImportReference(getTypeInfoAssembly));

            // create new[] { AssemblyCommonCategories.Engine }
            initializeMethodIL.Emit(OpCodes.Ldc_I4_1);
            initializeMethodIL.Emit(OpCodes.Newarr, assembly.MainModule.TypeSystem.String);
            initializeMethodIL.Emit(OpCodes.Dup);
            initializeMethodIL.Emit(OpCodes.Ldc_I4_0);
            initializeMethodIL.Emit(OpCodes.Ldstr, Core.Reflection.AssemblyCommonCategories.Engine);
            initializeMethodIL.Emit(OpCodes.Stelem_Ref);

            var assemblyRegistryRegisterMethodRef = assembly.MainModule.ImportReference(strideCoreModule.GetType("Stride.Core.Reflection.AssemblyRegistry").Methods.Single(x => x.Name == "Register" && x.Parameters[1].ParameterType.IsArray));
            initializeMethodIL.Emit(OpCodes.Call, assemblyRegistryRegisterMethodRef);

            initializeMethodIL.Emit(OpCodes.Ret);

            // Add AssemblySerializerFactoryAttribute
            var assemblySerializerFactoryAttribute = strideCoreModule.GetType("Stride.Core.Serialization.AssemblySerializerFactoryAttribute");
            assembly.CustomAttributes.Add(new CustomAttribute(assembly.MainModule.ImportReference(assemblySerializerFactoryAttribute.GetEmptyConstructor()))
            {
                Fields =
                {
                    new CustomAttributeNamedArgument("Type", new CustomAttributeArgument(typeTypeRef, serializerFactoryType)),
                }
            });

            serializationHash = hash.ComputeHash();
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
}
