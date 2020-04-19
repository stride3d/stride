// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Stride.Core.AssemblyProcessor
{
    internal class ParameterKeyProcessor : IAssemblyDefinitionProcessor
    {
        public bool Process(AssemblyProcessorContext context)
        {
            var assembly = context.Assembly;
            var fields = new List<FieldDefinition>();

            var mscorlibAssembly = CecilExtensions.FindCorlibAssembly(assembly);
            if (mscorlibAssembly == null)
                throw new InvalidOperationException("Missing mscorlib.dll from assembly");

            MethodDefinition parameterKeysMergeMethod = null;
            TypeDefinition assemblyEffectKeysAttributeType = null;
            var getTypeFromHandleMethod = new Lazy<MethodReference>(() =>
            {
                // Find Type.GetTypeFromHandle
                var typeType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(Type).FullName);
                return assembly.MainModule.ImportReference(typeType.Methods.First(x => x.Name == "GetTypeFromHandle"));
            });

            var effectKeysStaticConstructors = new List<MethodReference>();
            var effectKeysArrayElemementTypes = new HashSet<TypeReference>();

            foreach (var type in assembly.MainModule.GetTypes())
            {
                fields.Clear();

                foreach (var field in type.Fields.Where(x => x.IsStatic))
                {
                    var fieldBaseType = field.FieldType;
                    while (fieldBaseType != null)
                    {
                        if (fieldBaseType.FullName == "Stride.Rendering.ParameterKey")
                            break;

                        // TODO: Get PropertyKey.PropertyType instead
                        var genericInstance = fieldBaseType as GenericInstanceType;
                        if (genericInstance != null && genericInstance.ElementType.FullName == "Stride.Rendering.ParameterKey`1")
                        {
                            var genericArgument = genericInstance.GenericArguments.First();
                            if (genericArgument.IsArray)
                            {
                                effectKeysArrayElemementTypes.Add(genericArgument.GetElementType());
                            }
                        }

                        var resolvedFieldBaseType = fieldBaseType.Resolve();
                        if (resolvedFieldBaseType == null)
                        {
                            fieldBaseType = null;
                            break;
                        }

                        fieldBaseType = resolvedFieldBaseType.BaseType;
                    }

                    if (fieldBaseType == null)
                        continue;

                    fields.Add(field);
                }

                if (fields.Count == 0)
                    continue;

                // ParameterKey present means we should have a static cctor.
                var cctor = type.GetStaticConstructor();
                if (cctor == null)
                    continue;

                // Load necessary Stride methods/attributes
                if (parameterKeysMergeMethod == null)
                {
                    AssemblyDefinition strideEngineAssembly;
                    try
                    {
                        strideEngineAssembly = assembly.Name.Name == "Stride"
                            ? assembly
                            : context.AssemblyResolver.Resolve(new AssemblyNameReference("Stride", null));
                    }
                    catch (Exception)
                    {
                        context.Log.WriteLine("Error, cannot find [Stride] assembly for processing ParameterKeyProcessor");
                        // We can't generate an exception, so we are just returning. It means that Stride has not been generated so far.
                        return true;
                    }

                    var parameterKeysType = strideEngineAssembly.MainModule.GetTypes().First(x => x.Name == "ParameterKeys");
                    parameterKeysMergeMethod = parameterKeysType.Methods.First(x => x.Name == "Merge");
                    assemblyEffectKeysAttributeType = strideEngineAssembly.MainModule.GetTypes().First(x => x.Name == "AssemblyEffectKeysAttribute");
                }

                var cctorIL = cctor.Body.GetILProcessor();
                var cctorInstructions = cctor.Body.Instructions;

                var keyClassName = type.Name;
                if (keyClassName.EndsWith("Keys"))
                    keyClassName = keyClassName.Substring(0, keyClassName.Length - 4);

                keyClassName += '.';

                bool cctorModified = false;

                // Find field store instruction
                for (int i = 0; i < cctorInstructions.Count; ++i)
                {
                    var fieldInstruction = cctorInstructions[i];

                    if (fieldInstruction.OpCode == OpCodes.Stsfld
                        && fields.Contains(fieldInstruction.Operand))
                    {
                        var activeField = (FieldReference)fieldInstruction.Operand;

                        var nextInstruction = cctorInstructions[i + 1];
                        cctorIL.InsertBefore(nextInstruction, Instruction.Create(OpCodes.Ldsfld, activeField));
                        cctorIL.InsertBefore(nextInstruction, Instruction.Create(OpCodes.Ldtoken, type));
                        cctorIL.InsertBefore(nextInstruction, Instruction.Create(OpCodes.Call, getTypeFromHandleMethod.Value));
                        cctorIL.InsertBefore(nextInstruction, Instruction.Create(OpCodes.Ldstr, keyClassName + activeField.Name));
                        cctorIL.InsertBefore(nextInstruction, Instruction.Create(OpCodes.Call, assembly.MainModule.ImportReference(parameterKeysMergeMethod)));
                        cctorIL.InsertBefore(nextInstruction, Instruction.Create(OpCodes.Castclass, activeField.FieldType));
                        cctorIL.InsertBefore(nextInstruction, Instruction.Create(OpCodes.Stsfld, activeField));
                        i = cctorInstructions.IndexOf(nextInstruction);
                        cctorModified = true;
                    }
                }

                if (cctorModified)
                {
                    effectKeysStaticConstructors.Add(cctor);
                }
            }

            if (effectKeysStaticConstructors.Count > 0)
            {
                // Add [AssemblyEffectKeysAttribute] to the assembly
                assembly.CustomAttributes.Add(new CustomAttribute(assembly.MainModule.ImportReference(assemblyEffectKeysAttributeType.GetConstructors().First(x => !x.HasParameters))));

                // Get or create module static constructor
                var voidType = assembly.MainModule.TypeSystem.Void;
                var moduleClass = assembly.MainModule.Types.First(t => t.Name == "<Module>");
                var staticConstructor = moduleClass.GetStaticConstructor();
                if (staticConstructor == null)
                {
                    staticConstructor = new MethodDefinition(".cctor",
                                                             MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                                                             voidType);
                    staticConstructor.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));

                    moduleClass.Methods.Add(staticConstructor);
                }

                var il = staticConstructor.Body.GetILProcessor();

                var returnInstruction = staticConstructor.Body.Instructions.Last();
                var newReturnInstruction = Instruction.Create(returnInstruction.OpCode);
                newReturnInstruction.Operand = returnInstruction.Operand;

                returnInstruction.OpCode = OpCodes.Nop;
                returnInstruction.Operand = null;

                var typeType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(Type).FullName);
                var typeHandleProperty = typeType.Properties.First(x => x.Name == "TypeHandle");
                var getTypeHandleMethod = assembly.MainModule.ImportReference(typeHandleProperty.GetMethod);

                var runtimeHelpersType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(RuntimeHelpers).FullName);
                var runClassConstructorMethod = assembly.MainModule.ImportReference(runtimeHelpersType.Methods.Single(x => x.IsPublic && x.Name == "RunClassConstructor" && x.Parameters.Count == 1 && x.Parameters[0].ParameterType.FullName == typeof(RuntimeTypeHandle).FullName));

                // Call every key class static constructor from the module static constructor so that they are properly constructed (because accessing through reflection might cause problems)
                staticConstructor.Body.SimplifyMacros();
                foreach (var effectKeysStaticConstructor in effectKeysStaticConstructors)
                {
                    il.Append(Instruction.Create(OpCodes.Ldtoken, effectKeysStaticConstructor.DeclaringType));
                    il.Append(Instruction.Create(OpCodes.Call, getTypeFromHandleMethod.Value));
                    il.Append(Instruction.Create(OpCodes.Callvirt, getTypeHandleMethod));
                    il.Append(Instruction.Create(OpCodes.Call, runClassConstructorMethod));
                }

                if (effectKeysArrayElemementTypes.Count > 0)
                {
                    var methodImplAttributeType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(MethodImplAttribute).FullName);
                    var methodImplAttributesType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(MethodImplOptions).FullName);

                    var attribute = new CustomAttribute(methodImplAttributeType.GetConstructors().First(x => x.HasParameters && x.Parameters[0].ParameterType.FullName == methodImplAttributesType.FullName));
                    attribute.ConstructorArguments.Add(new CustomAttributeArgument(methodImplAttributesType, MethodImplOptions.NoOptimization));

                    staticConstructor.CustomAttributes.Add(attribute);
                }

                // Create instances of InternalValueArray<T>. Required for LLVM AOT
                foreach (var elementType in effectKeysArrayElemementTypes)
                {
                    var strideAssembly = assembly.Name.Name == "Stride" ? assembly : assembly.MainModule.AssemblyResolver.Resolve(new AssemblyNameReference("Stride", null));
                    var parameterCollectionType = strideAssembly.MainModule.GetTypeResolved("Stride.Rendering.ParameterCollection");
                    var internalValueArrayType = parameterCollectionType.NestedTypes.First(x => x.Name == "InternalValueArray`1");
                    var constructor = internalValueArrayType.GetConstructors().First();
                    var internalValueArrayConstructor = strideAssembly.MainModule.ImportReference(constructor).MakeGeneric(elementType);

                    il.Append(Instruction.Create(OpCodes.Ldc_I4_0));
                    il.Append(Instruction.Create(OpCodes.Newobj, internalValueArrayConstructor));
                    il.Append(Instruction.Create(OpCodes.Pop));
                }

                il.Append(newReturnInstruction);
                staticConstructor.Body.OptimizeMacros();
            }

            return true;
        }
    }
}
