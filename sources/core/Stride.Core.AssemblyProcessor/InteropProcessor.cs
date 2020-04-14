// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Stride.Core;
using CallSite = Mono.Cecil.CallSite;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Stride.Core.AssemblyProcessor
{
    internal class InteropProcessor : IAssemblyDefinitionProcessor
    {
        private readonly List<TypeDefinition> classToRemoveList = new List<TypeDefinition>();
        private AssemblyDefinition assembly;
        private TypeReference voidPointerType;
        private TypeReference intType;
        
        public bool Process(AssemblyProcessorContext context)
        {
            this.assembly = context.Assembly;
            // Import void* and int32 from assembly using mscorlib specific version (2.0 or 4.0 depending on assembly)
            voidPointerType = new PointerType(assembly.MainModule.TypeSystem.Void);
            intType = assembly.MainModule.TypeSystem.Int32;

            context.Log.WriteLine($"Patch for assembly [{assembly.FullName}]");
            foreach (var type in assembly.MainModule.Types)
                PatchType(type);

            // Remove All Interop classes
            foreach (var type in classToRemoveList)
                assembly.MainModule.Types.Remove(type);

            return true;
        }

        /// <summary>
        /// Creates a module init for a C# assembly.
        /// </summary>
        /// <param name="method">The method to add to the module init.</param>
        private void CreateModuleInit(MethodDefinition method)
        {
            const MethodAttributes ModuleInitAttributes = MethodAttributes.Private
                                                          | MethodAttributes.HideBySig
                                                          | MethodAttributes.Static
                                                          | MethodAttributes.Assembly
                                                          | MethodAttributes.SpecialName
                                                          | MethodAttributes.RTSpecialName;

            var moduleType = assembly.MainModule.GetTypeResolved("<Module>");

            // Get or create ModuleInit method
            var cctor = moduleType.Methods.FirstOrDefault(moduleTypeMethod => moduleTypeMethod.Name == ".cctor");
            if (cctor == null)
            {
                cctor = new MethodDefinition(".cctor", ModuleInitAttributes, method.ReturnType);
                moduleType.Methods.Add(cctor);
            }

            bool isCallAlreadyDone = cctor.Body.Instructions.Any(instruction => instruction.OpCode == OpCodes.Call && instruction.Operand == method);

            // If the method is not called, we can add it
            if (!isCallAlreadyDone)
            {
                var ilProcessor = cctor.Body.GetILProcessor();
                var retInstruction = cctor.Body.Instructions.FirstOrDefault(instruction => instruction.OpCode == OpCodes.Ret);
                var callMethod = ilProcessor.Create(OpCodes.Call, method);

                if (retInstruction == null)
                {
                    // If a ret instruction is not present, add the method call and ret
                    ilProcessor.Append(callMethod);
                    ilProcessor.Emit(OpCodes.Ret);
                }
                else
                {
                    // If a ret instruction is already present, just add the method to call before
                    ilProcessor.InsertBefore(retInstruction, callMethod);
                }
            }
        }

        /// <summary>
        /// Creates the write method with the following signature: 
        /// <code>
        /// public static unsafe void* Write&lt;T&gt;(void* pDest, ref T data) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method to patch</param>
        private void CreateWriteMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();
            var paramT = method.GenericParameters[0];
            // Preparing locals
            // local(0) int
            method.Body.Variables.Add(new VariableDefinition(intType));
            // local(1) T*
            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            // Push (0) pDest for memcpy
            gen.Emit(OpCodes.Ldarg_0);

            // fixed (void* pinnedData = &data[offset])
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stloc_1);

            // Push (1) pinnedData for memcpy
            gen.Emit(OpCodes.Ldloc_1);

            // totalSize = sizeof(T)
            gen.Emit(OpCodes.Sizeof, paramT);
            gen.Emit(OpCodes.Conv_I4);
            gen.Emit(OpCodes.Stloc_0);

            // Push (2) totalSize
            gen.Emit(OpCodes.Ldloc_0);

            // Emit cpblk
            EmitCpblk(method, gen);

            // Return pDest + totalSize
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Conv_I);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Add);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        private void ReplacePinStatement(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var previousInstruction = fixedtoPatch.Previous;
            int variableIndex;
            if (previousInstruction.OpCode == OpCodes.Ldloc_0)
            {
                variableIndex = 0;
            }
            else if (previousInstruction.OpCode == OpCodes.Ldloc_1)
            {
                variableIndex = 1;
            }
            else if (previousInstruction.OpCode == OpCodes.Ldloc_2)
            {
                variableIndex = 2;
            }
            else if (previousInstruction.OpCode == OpCodes.Ldloc_3)
            {
                variableIndex = 3;
            }
            else if (previousInstruction.OpCode == OpCodes.Ldloc_S)
            {
                variableIndex = ((VariableReference)previousInstruction.Operand).Index;
            }
            else if (previousInstruction.OpCode == OpCodes.Ldloc)
            {
                variableIndex = ((VariableReference)previousInstruction.Operand).Index;
            }
            else
            {
                throw new InvalidOperationException("Could not find a load operation right before Interop.Pin");
            }

            var variable = ilProcessor.Body.Variables[variableIndex];
            variable.VariableType = variable.VariableType.MakePinnedType();

            ilProcessor.Remove(previousInstruction);
            ilProcessor.Remove(fixedtoPatch);
        }

        private void ReplaceFixedStatement(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var paramT = ((GenericInstanceMethod)fixedtoPatch.Operand).GenericArguments[0];
            // Preparing locals
            // local(0) T& pinned
            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            int index = method.Body.Variables.Count - 1;

            Instruction ldlocFixed;
            Instruction stlocFixed;
            switch (index)
            {
                case 0:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_0);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_0);
                    break;
                case 1:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_1);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_1);
                    break;
                case 2:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_2);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_2);
                    break;
                case 3:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_3);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_3);
                    break;
                default:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc, index);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc, index);
                    break;
            }
            ilProcessor.InsertBefore(fixedtoPatch, stlocFixed);
            ilProcessor.Replace(fixedtoPatch, ldlocFixed);
        }

        private void ReplaceReadInline(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var paramT = ((GenericInstanceMethod)fixedtoPatch.Operand).GenericArguments[0];
            var copyInstruction = ilProcessor.Create(OpCodes.Ldobj, paramT);
            ilProcessor.Replace(fixedtoPatch, copyInstruction);
        }

        private void ReplaceCopyInline(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var paramT = ((GenericInstanceMethod)fixedtoPatch.Operand).GenericArguments[0];
            var copyInstruction = ilProcessor.Create(OpCodes.Cpobj, paramT);
            ilProcessor.Replace(fixedtoPatch, copyInstruction);
        }

        private void ReplaceSizeOfStructGeneric(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var paramT = ((GenericInstanceMethod)fixedtoPatch.Operand).GenericArguments[0];
            var copyInstruction = ilProcessor.Create(OpCodes.Sizeof, paramT);
            ilProcessor.Replace(fixedtoPatch, copyInstruction);
        }

        private void ReplacePinStructGeneric(MethodDefinition method, ILProcessor ilProcessor, Instruction pinToPatch)
        {
            // Next instruction should be a store to the variable that should be pinned
            var nextStoreInstruction = pinToPatch.Next;
            int variableIndex;
            if (nextStoreInstruction.OpCode == OpCodes.Stloc_0)
            {
                variableIndex = 0;
            }
            else if (nextStoreInstruction.OpCode == OpCodes.Stloc_1)
            {
                variableIndex = 1;
            }
            else if (nextStoreInstruction.OpCode == OpCodes.Stloc_2)
            {
                variableIndex = 2;
            }
            else if (nextStoreInstruction.OpCode == OpCodes.Stloc_3)
            {
                variableIndex = 3;
            }
            else if (nextStoreInstruction.OpCode == OpCodes.Stloc_S)
            {
                variableIndex = ((VariableReference)nextStoreInstruction.Operand).Index;
            }
            else if (nextStoreInstruction.OpCode == OpCodes.Stloc)
            {
                variableIndex = ((VariableReference)nextStoreInstruction.Operand).Index;
            }
            else
            {
                throw new InvalidOperationException("Could not find a store operation right after Interop.Pin");
            }

            // Transform variable from:
            //   valuetype Struct s
            // to:
            //   valuetype Struct& modopt([mscorlib]System.Runtime.CompilerServices.IsExplicitlyDereferenced) pinned s,
            var variable = method.Body.Variables[variableIndex];
            variable.VariableType = variable.VariableType
                .MakeByReferenceType()
                //.MakeOptionalModifierType(typeof(IsExplicitlyDereferenced))
                .MakePinnedType();

            // Remove call to Interop.Pin:
            ilProcessor.Remove(pinToPatch);

            // Transform all ldloca with this variable into ldloc:
            for (int index = 0; index < ilProcessor.Body.Instructions.Count; index++)
            {
                var instruction = ilProcessor.Body.Instructions[index];
                if (instruction.OpCode == OpCodes.Ldloca && ((VariableReference)instruction.Operand).Index == variableIndex)
                {
                    instruction.OpCode = OpCodes.Ldloc;
                }
                else if (instruction.OpCode == OpCodes.Ldloca_S && ((VariableReference)instruction.Operand).Index == variableIndex)
                {
                    instruction.OpCode = OpCodes.Ldloc_S;
                }
            }
        }

        private void ReplaceIncrementPinnedStructGeneric(MethodDefinition method, ILProcessor ilProcessor, Instruction incrementPinnedToPatch)
        {
            var paramT = ((GenericInstanceMethod)incrementPinnedToPatch.Operand).GenericArguments[0];

            var sizeOfInst = ilProcessor.Create(OpCodes.Sizeof, paramT);

            ilProcessor.Replace(incrementPinnedToPatch, sizeOfInst);
            ilProcessor.InsertAfter(sizeOfInst, ilProcessor.Create(OpCodes.Add));
        }

        private void ReplaceAddPinnedStructGeneric(MethodDefinition method, ILProcessor ilProcessor, Instruction incrementPinnedToPatch)
        {
            var paramT = ((GenericInstanceMethod)incrementPinnedToPatch.Operand).GenericArguments[0];

            var sizeOfInst = ilProcessor.Create(OpCodes.Sizeof, paramT);

            ilProcessor.Replace(incrementPinnedToPatch, sizeOfInst);
            var instructionAdd = ilProcessor.Create(OpCodes.Add);
            ilProcessor.InsertAfter(sizeOfInst, instructionAdd);
            ilProcessor.InsertBefore(instructionAdd, ilProcessor.Create(OpCodes.Mul));
        }

        /// <summary>
        /// Creates the cast  method with the following signature:
        /// <code>
        /// public static unsafe void* Cast&lt;T&gt;(ref T data) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method cast.</param>
        private void CreateCastMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();

            gen.Emit(OpCodes.Ldarg_0);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates the cast  method with the following signature:
        /// <code>
        /// public static TCAST[] CastArray&lt;TCAST, T&gt;(T[] arrayData) where T : struct where TCAST : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method cast array.</param>
        private void CreateCastArrayMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();

            gen.Emit(OpCodes.Ldarg_0);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        private void ReplaceFixedArrayStatement(MethodDefinition method, ILProcessor ilProcessor, Instruction fixedtoPatch)
        {
            var paramT = ((GenericInstanceMethod)fixedtoPatch.Operand).GenericArguments[0];
            // Preparing locals
            // local(0) T*
            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            int index = method.Body.Variables.Count - 1;

            Instruction ldlocFixed;
            Instruction stlocFixed;
            switch (index)
            {
                case 0:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_0);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_0);
                    break;
                case 1:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_1);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_1);
                    break;
                case 2:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_2);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_2);
                    break;
                case 3:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_3);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_3);
                    break;
                default:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc, index);
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc, index);
                    break;
            }

            var instructionLdci40 = ilProcessor.Create(OpCodes.Ldc_I4_0);
            ilProcessor.InsertBefore(fixedtoPatch, instructionLdci40);
            var instructionLdElema = ilProcessor.Create(OpCodes.Ldelema, paramT);
            ilProcessor.InsertBefore(fixedtoPatch, instructionLdElema);
            ilProcessor.InsertBefore(fixedtoPatch, stlocFixed);
            ilProcessor.Replace(fixedtoPatch, ldlocFixed);
        }

        /// <summary>
        /// Creates the write range method with the following signature:
        /// <code>
        /// public static unsafe void* Write&lt;T&gt;(void* pDest, T[] data, int offset, int count) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method copy struct.</param>
        private void CreateWriteRangeMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();
            var paramT = method.GenericParameters[0];
            // Preparing locals
            // local(0) int
            method.Body.Variables.Add(new VariableDefinition(intType));
            // local(1) T*
            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            // Push (0) pDest for memcpy
            gen.Emit(OpCodes.Ldarg_0);

            // fixed (void* pinnedData = &data[offset])
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Ldelema, paramT);
            gen.Emit(OpCodes.Stloc_1);

            // Push (1) pinnedData for memcpy
            gen.Emit(OpCodes.Ldloc_1);

            // totalSize = sizeof(T) * count
            gen.Emit(OpCodes.Sizeof, paramT);
            gen.Emit(OpCodes.Conv_I4);
            gen.Emit(OpCodes.Ldarg_3);
            gen.Emit(OpCodes.Mul);
            gen.Emit(OpCodes.Stloc_0);

            // Push (2) totalSize
            gen.Emit(OpCodes.Ldloc_0);

            // Emit cpblk
            EmitCpblk(method, gen);

            // Return pDest + totalSize
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Conv_I);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Add);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates the read method with the following signature:
        /// <code>
        /// public static unsafe void* Read&lt;T&gt;(void* pSrc, ref T data) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method copy struct.</param>
        private void CreateReadMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();
            var paramT = method.GenericParameters[0];

            // Preparing locals
            // local(0) int
            method.Body.Variables.Add(new VariableDefinition(intType));
            // local(1) T*

            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            // fixed (void* pinnedData = &data[offset])
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stloc_1);

            // Push (0) pinnedData for memcpy
            gen.Emit(OpCodes.Ldloc_1);

            // Push (1) pSrc for memcpy
            gen.Emit(OpCodes.Ldarg_0);

            // totalSize = sizeof(T)
            gen.Emit(OpCodes.Sizeof, paramT);
            gen.Emit(OpCodes.Conv_I4);
            gen.Emit(OpCodes.Stloc_0);

            // Push (2) totalSize
            gen.Emit(OpCodes.Ldloc_0);

            // Emit cpblk
            EmitCpblk(method, gen);

            // Return pDest + totalSize
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Conv_I);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Add);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates the read method with the following signature:
        /// <code>
        /// public static unsafe void Read&lt;T&gt;(void* pSrc, ref T data) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method copy struct.</param>
        private void CreateReadRawMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();
            var paramT = method.GenericParameters[0];

            // Push (1) pSrc for memcpy
            gen.Emit(OpCodes.Cpobj);

        }

        /// <summary>
        /// Creates the read range method with the following signature:
        /// <code>
        /// public static unsafe void* Read&lt;T&gt;(void* pSrc, T[] data, int offset, int count) where T : struct
        /// </code>
        /// </summary>
        /// <param name="method">The method copy struct.</param>
        private void CreateReadRangeMethod(MethodDefinition method)
        {
            method.Body.Instructions.Clear();
            method.Body.InitLocals = true;

            var gen = method.Body.GetILProcessor();
            var paramT = method.GenericParameters[0];
            // Preparing locals
            // local(0) int
            method.Body.Variables.Add(new VariableDefinition(intType));
            // local(1) T*
            method.Body.Variables.Add(new VariableDefinition(new PinnedType(new ByReferenceType(paramT))));

            // fixed (void* pinnedData = &data[offset])
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Ldelema, paramT);
            gen.Emit(OpCodes.Stloc_1);

            // Push (0) pinnedData for memcpy
            gen.Emit(OpCodes.Ldloc_1);

            // Push (1) pDest for memcpy
            gen.Emit(OpCodes.Ldarg_0);

            // totalSize = sizeof(T) * count
            gen.Emit(OpCodes.Sizeof, paramT);
            gen.Emit(OpCodes.Conv_I4);
            gen.Emit(OpCodes.Ldarg_3);
            gen.Emit(OpCodes.Conv_I4);
            gen.Emit(OpCodes.Mul);
            gen.Emit(OpCodes.Stloc_0);

            // Push (2) totalSize
            gen.Emit(OpCodes.Ldloc_0);

            // Emit cpblk
            EmitCpblk(method, gen);

            // Return pDest + totalSize
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Conv_I);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Add);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates the memcpy method with the following signature:
        /// <code>
        /// public static unsafe void memcpy(void* pDest, void* pSrc, int count)
        /// </code>
        /// </summary>
        /// <param name="methodCopyStruct">The method copy struct.</param>
        private void CreateMemcpy(MethodDefinition methodCopyStruct)
        {
            methodCopyStruct.Body.Instructions.Clear();

            var gen = methodCopyStruct.Body.GetILProcessor();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Unaligned, (byte)1);       // unaligned to 1
            gen.Emit(OpCodes.Cpblk);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Creates the memset method with the following signature:
        /// <code>
        /// public static unsafe void memset(void* pDest, byte value, int count)
        /// </code>
        /// </summary>
        /// <param name="methodSetStruct">The method set struct.</param>
        private void CreateMemset(MethodDefinition methodSetStruct)
        {
            methodSetStruct.Body.Instructions.Clear();

            var gen = methodSetStruct.Body.GetILProcessor();

            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Ldarg_2);
            gen.Emit(OpCodes.Unaligned, (byte)1);       // unaligned to 1
            gen.Emit(OpCodes.Initblk);

            // Ret
            gen.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// Emits the cpblk method, supporting x86 and x64 platform.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="gen">The gen.</param>
        private void EmitCpblk(MethodDefinition method, ILProcessor gen)
        {
            var cpblk = gen.Create(OpCodes.Cpblk);
            //gen.Emit(OpCodes.Sizeof, voidPointerType);
            //gen.Emit(OpCodes.Ldc_I4_8);
            //gen.Emit(OpCodes.Bne_Un_S, cpblk);
            gen.Emit(OpCodes.Unaligned, (byte)1);       // unaligned to 1
            gen.Append(cpblk);

        }

        private List<string> GetSharpDXAttributes(MethodDefinition method)
        {
            var attributes = new List<string>();
            foreach (var customAttribute in method.CustomAttributes)
            {
                if (customAttribute.AttributeType.FullName == "SharpDX.TagAttribute")
                {
                    var value = customAttribute.ConstructorArguments[0].Value;
                    attributes.Add(value == null ? string.Empty : value.ToString());
                }
            }

            return attributes;
        }

        /// <summary>
        /// Patches the method.
        /// </summary>
        /// <param name="method">The method.</param>
        bool PatchMethod(MethodDefinition method)
        {
            bool isSharpJit = false;

            var attributes = this.GetSharpDXAttributes(method);
            if (attributes.Contains("SharpDX.ModuleInit"))
            {
                CreateModuleInit(method);
            }

            if (method.DeclaringType.Name == "Interop")
            {
                if (method.Name == "memcpy")
                {
                    CreateMemcpy(method);
                }
                else if (method.Name == "memset")
                {
                    CreateMemset(method);
                }
                else if ((method.Name == "Cast") || (method.Name == "CastOut"))
                {
                    CreateCastMethod(method);
                }
                else if (method.Name == "CastArray")
                {
                    CreateCastArrayMethod(method);
                }
                else if (method.Name == "Read" || (method.Name == "ReadOut") || (method.Name == "Read2D"))
                {
                    if (method.Parameters.Count == 2)
                        CreateReadMethod(method);
                    else
                        CreateReadRangeMethod(method);
                }
                else if (method.Name == "Write" || (method.Name == "Write2D"))
                {
                    if (method.Parameters.Count == 2)
                        CreateWriteMethod(method);
                    else
                        CreateWriteRangeMethod(method);
                }
            }
            else if (method.HasBody)
            {
                var ilProcessor = method.Body.GetILProcessor();

                var instructions = method.Body.Instructions;
                Instruction instruction = null;
                Instruction previousInstruction;
                bool changes = false;
                for (int i = 0; i < instructions.Count; i++)
                {
                    previousInstruction = instruction;
                    instruction = instructions[i];

                    if (instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference)
                    {
                        var methodDescription = (MethodReference)instruction.Operand;

                        if (methodDescription is MethodDefinition)
                        {
                            foreach (var customAttribute in ((MethodDefinition)methodDescription).CustomAttributes)
                            {
                                if (customAttribute.AttributeType.FullName == typeof(ObfuscationAttribute).FullName)
                                {
                                    foreach (var arg in customAttribute.Properties)
                                    {
                                        if (arg.Name == "Feature" && arg.Argument.Value != null)
                                        {
                                            var customValue = arg.Argument.Value.ToString();
                                            if (customValue.StartsWith("SharpJit."))
                                            {
                                                isSharpJit = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (isSharpJit) break;
                            }
                        }

                        if (!isSharpJit)
                        {
                            if (methodDescription.Name.StartsWith("Calli") && methodDescription.DeclaringType.Name == "LocalInterop")
                            {
                                var callSite = new CallSite(methodDescription.ReturnType) { CallingConvention = MethodCallingConvention.StdCall };
                                // Last parameter is the function ptr, so we don't add it as a parameter for calli
                                // as it is already an implicit parameter for calli
                                for (int j = 0; j < methodDescription.Parameters.Count - 1; j++)
                                {
                                    var parameterDefinition = methodDescription.Parameters[j];
                                    callSite.Parameters.Add(parameterDefinition);
                                }

                                // Create calli Instruction
                                var callIInstruction = ilProcessor.Create(OpCodes.Calli, callSite);

                                // Replace instruction
                                ilProcessor.Replace(instruction, callIInstruction);
                            }
                            else if (methodDescription.DeclaringType.Name == "Interop")
                            {
                                changes = true;
                                if (methodDescription.FullName.Contains("Fixed"))
                                {
                                    if (methodDescription.Parameters[0].ParameterType.IsArray)
                                    {
                                        ReplaceFixedArrayStatement(method, ilProcessor, instruction);
                                    }
                                    else
                                    {
                                        ReplaceFixedStatement(method, ilProcessor, instruction);
                                    }
                                }
                                else if (methodDescription.Name.StartsWith("ReadInline"))
                                {
                                    this.ReplaceReadInline(method, ilProcessor, instruction);
                                }
                                else if (methodDescription.Name.StartsWith("CopyInline") || methodDescription.Name.StartsWith("WriteInline"))
                                {
                                    this.ReplaceCopyInline(method, ilProcessor, instruction);
                                }
                                else if (methodDescription.Name.StartsWith("SizeOf"))
                                {
                                    this.ReplaceSizeOfStructGeneric(method, ilProcessor, instruction);
                                }
                                else if (methodDescription.Name.StartsWith("Pin"))
                                {
                                    if (methodDescription.Parameters[0].ParameterType.IsByReference)
                                    {
                                        this.ReplacePinStructGeneric(method, ilProcessor, instruction);
                                    }
                                    else
                                    {
                                        this.ReplacePinStatement(method, ilProcessor, instruction);
                                    }
                                }
                                else if (methodDescription.Name.StartsWith("IncrementPinned"))
                                {
                                    this.ReplaceIncrementPinnedStructGeneric(method, ilProcessor, instruction);
                                }
                                else if (methodDescription.Name.StartsWith("AddPinned"))
                                {
                                    this.ReplaceAddPinnedStructGeneric(method, ilProcessor, instruction);
                                }
                            }
                        }
                    }
                }

                if (changes)
                {
                    // Compute offsets again (otherwise functions such as switch might fail).
                    method.Body.OptimizeMacros();
                }
            }
            return isSharpJit;
        }

        bool containsSharpJit;

        /// <summary>
        /// Patches the type.
        /// </summary>
        /// <param name="type">The type.</param>
        void PatchType(TypeDefinition type)
        {
            // Patch methods
            foreach (var method in type.Methods)
                if (PatchMethod(method))
                    containsSharpJit = true;

            // LocalInterop will be removed after the patch only for non SharpJit code
            if (!containsSharpJit && type.Name == "LocalInterop")
                classToRemoveList.Add(type);

            // Patch nested types
            foreach (var typeDefinition in type.NestedTypes)
                PatchType(typeDefinition);
        }
    }
}
