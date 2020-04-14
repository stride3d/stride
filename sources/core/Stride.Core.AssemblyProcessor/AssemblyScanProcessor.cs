// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Stride.Core.AssemblyProcessor
{
    internal class AssemblyScanProcessor : IAssemblyDefinitionProcessor
    {
        private static readonly string attributeUsageTypeName = typeof(AttributeUsageAttribute).FullName;

        public AssemblyScanProcessor()
        {
        }

        public bool Process(AssemblyProcessorContext context)
        {
            var registry = new AssemblyScanRegistry();
            foreach (var type in context.Assembly.MainModule.GetAllTypes())
            {
                // Ignore interface types as well as types with generics
                // Note: we could support generic types at some point but we probably need
                //       to get static generic instantiation type list from serializer code generator
                if (type.IsInterface || type.HasGenericParameters)
                    continue;

                var currentType = type;
                // Scan type and parent types
                while (currentType != null)
                {
                    // Scan interfaces
                    foreach (var @interface in currentType.Interfaces)
                    {
                        ScanAttributes(context.Log, registry, @interface.InterfaceType, type);
                    }

                    ScanAttributes(context.Log, registry, currentType, type);
                    currentType = currentType.BaseType?.Resolve();
                }
            }

            if (registry.HasScanTypes)
            {
                // This code should mirror what AssemblyScanCodeGenerator.tt generates
                var assembly = context.Assembly;

                var strideCoreModule = assembly.GetStrideCoreModule();
                var assemblyRegistryType = strideCoreModule.GetType("Stride.Core.Reflection.AssemblyRegistry");

                // Generate code
                var assemblyScanType = new TypeDefinition("Stride.Core.Serialization.AssemblyScan",
                    Utilities.BuildValidClassName(assembly.Name.Name) + "AssemblyScan",
                    TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
                    TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract,
                    assembly.MainModule.TypeSystem.Object);
                assembly.MainModule.Types.Add(assemblyScanType);

                // Create Initialize method
                var initializeMethod = new MethodDefinition("Initialize",
                    MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.Static,
                    assembly.MainModule.TypeSystem.Void);
                assemblyScanType.Methods.Add(initializeMethod);

                // Make sure it is called at module startup
                initializeMethod.AddModuleInitializer(-2000);

                var mscorlibAssembly = CecilExtensions.FindCorlibAssembly(assembly);
                var collectionAssembly = CecilExtensions.FindCollectionsAssembly(assembly);
                var reflectionAssembly = CecilExtensions.FindReflectionAssembly(assembly);

                // Type
                var typeType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(Type).FullName);
                var typeTypeRef = assembly.MainModule.ImportReference(typeType);
                var getTypeFromHandleMethod = typeType.Methods.First(x => x.Name == nameof(Type.GetTypeFromHandle));
                var getTokenInfoExMethod = reflectionAssembly.MainModule.GetTypeResolved("System.Reflection.IntrospectionExtensions").Resolve().Methods.First(x => x.Name == nameof(IntrospectionExtensions.GetTypeInfo));
                var typeInfoType = reflectionAssembly.MainModule.GetTypeResolved(typeof(TypeInfo).FullName);
                // Note: TypeInfo.Assembly/Module could be on the type itself or on its parent MemberInfo depending on runtime
                var getTypeInfoAssembly = typeInfoType.Properties.Concat(typeInfoType.BaseType.Resolve().Properties).First(x => x.Name == nameof(TypeInfo.Assembly)).GetMethod;

                // List<Type>
                var listType = collectionAssembly.MainModule.GetTypeResolved(typeof(List<>).FullName);
                var listTypeTypeRef = assembly.MainModule.ImportReference(listType).MakeGenericType(typeTypeRef);
                // Dictionary<Type, List<Type>>
                var dictionaryType = collectionAssembly.MainModule.GetType(typeof(Dictionary<,>).FullName);

                var initializeMethodIL = initializeMethod.Body.GetILProcessor();

                // AssemblyRegistry.RegisterScanTypes(typeof(AssemblyScanType).GetTypeInfo().Assembly, dictionary);
                initializeMethodIL.Emit(OpCodes.Ldtoken, assemblyScanType);
                initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod));
                initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTokenInfoExMethod));
                initializeMethodIL.Emit(OpCodes.Callvirt, assembly.MainModule.ImportReference(getTypeInfoAssembly));

                // dictionary = new Dictionary<Type, List<Type>>();
                initializeMethodIL.Emit(OpCodes.Newobj, assembly.MainModule.ImportReference(dictionaryType.GetEmptyConstructor()).MakeGeneric(typeTypeRef, listTypeTypeRef));
                foreach (var scanTypeEntry in registry.ScanTypes)
                {
                    initializeMethodIL.Emit(OpCodes.Dup);

                    // typeof(X)
                    initializeMethodIL.Emit(OpCodes.Ldtoken, assembly.MainModule.ImportReference(scanTypeEntry.Key.Resolve()));
                    initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod));

                    // new List<Type>();
                    initializeMethodIL.Emit(OpCodes.Newobj, assembly.MainModule.ImportReference(listType.GetEmptyConstructor()).MakeGeneric(typeTypeRef));

                    foreach (var scanType in scanTypeEntry.Value)
                    {
                        initializeMethodIL.Emit(OpCodes.Dup);
                        initializeMethodIL.Emit(OpCodes.Ldtoken, assembly.MainModule.ImportReference(scanType));
                        initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod));
                        initializeMethodIL.Emit(OpCodes.Callvirt, assembly.MainModule.ImportReference(listType.Methods.First(x => x.Name == "Add")).MakeGeneric(typeTypeRef));
                    }

                    initializeMethodIL.Emit(OpCodes.Callvirt, assembly.MainModule.ImportReference(dictionaryType.Methods.First(x => x.Name == "Add")).MakeGeneric(typeTypeRef, listTypeTypeRef));
                }

                initializeMethodIL.Emit(OpCodes.Newobj, assembly.MainModule.ImportReference(assemblyRegistryType.NestedTypes.First(x => x.Name == "ScanTypes").Methods.Single(x => x.IsConstructor && x.Parameters.Count == 1)));
                initializeMethodIL.Emit(OpCodes.Call, assembly.MainModule.ImportReference(assemblyRegistryType.Methods.First(x => x.Name == "RegisterScanTypes")));

                initializeMethodIL.Emit(OpCodes.Ret);

                //var assemblyScanCodeGenerator = new AssemblyScanCodeGenerator(assembly, registry);
                //sourceCodeRegisterAction(assemblyScanCodeGenerator.TransformText(), "AssemblyScan");
            }

            return registry.HasScanTypes;
        }

        private static void ScanAttributes(TextWriter log, AssemblyScanRegistry assemblyScanRegistry, TypeReference scanType, TypeDefinition type)
        {
            foreach (var attribute in scanType.Resolve().CustomAttributes)
            {
                // Check if scanned type has any AssemblyScanAttribute attribute
                if (attribute.AttributeType.FullName == "Stride.Core.Reflection.AssemblyScanAttribute")
                {
                    RegisterType(log, assemblyScanRegistry, type, scanType);
                }

                // Check if the attribute type has any AssemblyScanAttribute attribute
                // This allows to create custom attributes and scan for them
                foreach (var attributeAttribute in attribute.AttributeType.Resolve().CustomAttributes)
                {
                    var hasAssemblyScanAttribute = false;
                    if (attributeAttribute.AttributeType.FullName == "Stride.Core.Reflection.AssemblyScanAttribute")
                    {
                        hasAssemblyScanAttribute = true;
                    }
                    else if (attributeAttribute.AttributeType.FullName == attributeUsageTypeName)
                    {
                        // If AttributeUsage has Inherited = false, let's skip right away if we are not processing main type
                        if (scanType != type && attributeAttribute.HasProperties
                            && attributeAttribute.Properties.FirstOrDefault(x => x.Name == nameof(AttributeUsageAttribute.Inherited)).Argument.Value as bool? == false)
                            break;
                    }

                    if (hasAssemblyScanAttribute)
                    {
                        RegisterType(log, assemblyScanRegistry, type, attribute.AttributeType);
                    }
                }
            }
        }

        private static void RegisterType(TextWriter log, AssemblyScanRegistry assemblyScanRegistry, TypeDefinition type, TypeReference scanType)
        {
            // Nested type needs to be either public or internal otherwise we can't access them from other classes
            if (type.IsNested && !type.IsNestedPublic && !type.IsNestedAssembly)
            {
                log.WriteLine($"{nameof(AssemblyScanProcessor)}: Can't register type [{type}] for scan type [{scanType}] because it is a nested private type");
                return;
            }

            assemblyScanRegistry.Register(type, scanType);
        }
    }
}
