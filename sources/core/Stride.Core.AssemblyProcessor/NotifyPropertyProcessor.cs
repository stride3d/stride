// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace Stride.Core.AssemblyProcessor
{
    /// <summary>
    /// This class is no longer used now.
    /// TODO: Check if we need to keep this around
    /// </summary>
    internal class NotifyPropertyProcessor : IAssemblyDefinitionProcessor
    {
        private TypeReference voidType, stringType, objectType;

        public FieldReference GetPropertyChangedField(TypeDefinition typeDefinition)
        {
            // Not sure whether it's better to detect through name+type, or actual INotifyPropertyChanged interface (which we might not want to add sometime)
            var propertyChangedField = typeDefinition.Fields.FirstOrDefault(x => x.FieldType.FullName == typeof(System.ComponentModel.PropertyChangedEventHandler).FullName && x.Name == "PropertyChanged");
            if (propertyChangedField != null)
                return propertyChangedField;

            // Go through inheritance hierarchy
            //if (typeDefinition.BaseType != null)
            //    return GetPropertyChangedField(typeDefinition.BaseType.Resolve());

            return null;
        }

        public MethodReference GetGetPropertyChangedMethod(AssemblyDefinition assembly, TypeDefinition typeDefinition)
        {
            var method = typeDefinition.Methods.FirstOrDefault(x => x.Name == "GetPropertyChanged");
            if (method != null)
                return assembly.MainModule.ImportReference(method);

            if (typeDefinition.BaseType != null)
                return GetGetPropertyChangedMethod(assembly, typeDefinition.BaseType.Resolve());

            return null;
        }

        public MethodReference GetOrCreateGetPropertyChangedMethod(AssemblyDefinition assembly, TypeDefinition typeDefinition, FieldReference propertyChangedField)
        {
            var methodReference = GetGetPropertyChangedMethod(assembly, typeDefinition);
            if (methodReference != null)
                return methodReference;

            var method = new MethodDefinition("GetPropertyChanged", MethodAttributes.Family, propertyChangedField.FieldType);

            var bodyInstructions = method.Body.Instructions;

            // return PropertyChanged
            bodyInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            bodyInstructions.Add(Instruction.Create(OpCodes.Ldfld, propertyChangedField));
            bodyInstructions.Add(Instruction.Create(OpCodes.Ret));

            typeDefinition.Methods.Add(method);

            return method;
        }
        
        public bool Process(AssemblyProcessorContext context)
        {
            var assembly = context.Assembly;
            var mscorlibAssembly = CecilExtensions.FindCorlibAssembly(assembly);
            if (mscorlibAssembly == null)
                throw new InvalidOperationException("Missing mscorlib.dll from assembly");

            // For now, use import, but this can cause mixed framework versions when processing an assembly with a different framework version.
            voidType = assembly.MainModule.TypeSystem.Void;
            stringType = assembly.MainModule.TypeSystem.String;
            objectType = assembly.MainModule.TypeSystem.Object;
            var propertyInfoType = assembly.MainModule.ImportReference(mscorlibAssembly.MainModule.GetTypeResolved(typeof(PropertyInfo).FullName));
            var typeType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(Type).FullName);


            TypeDefinition propertyChangedExtendedEventArgsType;

            ModuleDefinition strideCoreModule;
            try
            {
                strideCoreModule = assembly.GetStrideCoreModule();
            }
            catch (Exception)
            {
                return true;
            }

            propertyChangedExtendedEventArgsType = strideCoreModule.GetTypes().First(x => x.Name == "PropertyChangedExtendedEventArgs").Resolve();

            var typeTokenInfoEx = mscorlibAssembly.MainModule.GetTypeResolved("System.Reflection.TypeInfo").Resolve();
            var getPropertyMethod = typeTokenInfoEx.Methods.First(x => x.Name == "GetDeclaredProperty" && x.Parameters.Count == 1);
            var getTypeFromHandleMethod = typeType.Methods.First(x => x.Name == "GetTypeFromHandle");
            var getTokenInfoExMethod = CecilExtensions.FindReflectionAssembly(assembly).MainModule.GetTypeResolved("System.Reflection.IntrospectionExtensions").Resolve().Methods.First(x => x.Name == "GetTypeInfo");
            
            var propertyChangedExtendedEventArgsConstructor = assembly.MainModule.ImportReference(propertyChangedExtendedEventArgsType.Methods.First(x => x.IsConstructor));

            bool modified = false;

            foreach (var type in assembly.MainModule.GetTypes())
            {
                MethodReference getPropertyChangedMethod;

                getPropertyChangedMethod = GetGetPropertyChangedMethod(assembly, type);
                //var propertyChangedField = GetPropertyChangedField(type);
                //if (propertyChangedField == null)
                //    continue;

                var propertyChangedField = GetPropertyChangedField(type);

                if (getPropertyChangedMethod == null && propertyChangedField == null)
                    continue;

                TypeReference propertyChangedFieldType;

                if (getPropertyChangedMethod == null)
                {
                    modified = true;
                    getPropertyChangedMethod = GetOrCreateGetPropertyChangedMethod(assembly, type, propertyChangedField);
                }

                if (propertyChangedField != null)
                {
                    propertyChangedField = assembly.MainModule.ImportReference(propertyChangedField);
                    propertyChangedFieldType = propertyChangedField.FieldType;
                }
                else
                {
                    propertyChangedFieldType = getPropertyChangedMethod.ReturnType;
                }

                // Add generic to declaring type
                if (getPropertyChangedMethod.DeclaringType.HasGenericParameters)
                    getPropertyChangedMethod = getPropertyChangedMethod.MakeGeneric(getPropertyChangedMethod.DeclaringType.GenericParameters.ToArray());

                var propertyChangedInvoke = assembly.MainModule.ImportReference(propertyChangedFieldType.Resolve().Methods.First(x => x.Name == "Invoke"));

                foreach (var property in type.Properties)
                {
                    if (property.SetMethod == null || !property.HasThis)
                        continue;

                    MethodReference propertyGetMethod = property.GetMethod;

                    // Only patch properties that have a public Getter
                    var methodDefinition = propertyGetMethod.Resolve();
                    if ((methodDefinition.Attributes & MethodAttributes.Public) != MethodAttributes.Public)
                    {
                        continue;
                    }

                    // Add generic to declaring type
                    if (propertyGetMethod.DeclaringType.HasGenericParameters)
                        propertyGetMethod = propertyGetMethod.MakeGeneric(propertyGetMethod.DeclaringType.GenericParameters.ToArray());

                    //var versionableAttribute = property.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == typeof(VersionableAttribute).FullName);
                    //if (versionableAttribute == null)
                    //    continue;

                    modified = true;

                    FieldReference staticField = new FieldDefinition(property.Name + "PropertyInfo", FieldAttributes.Static | FieldAttributes.Private | FieldAttributes.InitOnly, propertyInfoType);
                    type.Fields.Add((FieldDefinition)staticField);

                    // Add generic to declaring type
                    if (staticField.DeclaringType.HasGenericParameters)
                        staticField = staticField.MakeGeneric(staticField.DeclaringType.GenericParameters.ToArray());

                    // In static constructor, find PropertyInfo and store it in static field
                    {
                        var staticConstructor = type.GetStaticConstructor();
                        if (staticConstructor == null)
                        {
                            staticConstructor = new MethodDefinition(".cctor",
                                                                     MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                                                                     voidType);
                            staticConstructor.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));

                            type.Methods.Add(staticConstructor);
                        }


                        VariableReference localTokenEx = null;
                        int localTokenExIndex = 0;
                        for (int i = 0; i < staticConstructor.Body.Variables.Count; i++)
                        {
                            var localVar = staticConstructor.Body.Variables[i];
                            if (localVar.VariableType.FullName == typeTokenInfoEx.FullName)
                            {
                                localTokenEx = localVar;
                                localTokenExIndex = i;
                                break;
                            }
                        }

                        if (localTokenEx == null)
                        {
                            localTokenEx = new VariableDefinition(assembly.MainModule.ImportReference(typeTokenInfoEx));
                            staticConstructor.Body.Variables.Add((VariableDefinition)localTokenEx);
                            localTokenExIndex = staticConstructor.Body.Variables.Count - 1;
                        }

                        var ilProcessor = staticConstructor.Body.GetILProcessor();
                        var returnInstruction = staticConstructor.Body.Instructions.Last();

                        var newReturnInstruction = Instruction.Create(returnInstruction.OpCode);
                        newReturnInstruction.Operand = returnInstruction.Operand;

                        returnInstruction.OpCode = OpCodes.Nop;
                        returnInstruction.Operand = null;

                        // Find PropertyInfo and store it in static field
                        ilProcessor.Append(Instruction.Create(OpCodes.Ldtoken, type));
                        ilProcessor.Append(Instruction.Create(OpCodes.Call, assembly.MainModule.ImportReference(getTypeFromHandleMethod)));
                        ilProcessor.Append(Instruction.Create(OpCodes.Call, assembly.MainModule.ImportReference(getTokenInfoExMethod)));
                        ilProcessor.Append(LocationToStloc(ilProcessor, localTokenExIndex));
                        ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloca_S, (byte)localTokenExIndex));
                        ilProcessor.Append(Instruction.Create(OpCodes.Ldstr, property.Name));
                        ilProcessor.Append(Instruction.Create(OpCodes.Call, assembly.MainModule.ImportReference(getPropertyMethod)));
                        ilProcessor.Append(Instruction.Create(OpCodes.Stsfld, staticField));

                        ilProcessor.Append(newReturnInstruction);
                    }
                    
                    {
                        var ilProcessor = property.SetMethod.Body.GetILProcessor();
                        var returnInstruction = property.SetMethod.Body.Instructions.Last();

                        var firstInstruction = property.SetMethod.Body.Instructions[0];

                        if (property.SetMethod.Body.Instructions[0].OpCode != OpCodes.Nop)
                            ilProcessor.InsertBefore(property.SetMethod.Body.Instructions[0], Instruction.Create(OpCodes.Nop));

                        var newReturnInstruction = Instruction.Create(returnInstruction.OpCode);
                        newReturnInstruction.Operand = returnInstruction.Operand;

                        returnInstruction.OpCode = OpCodes.Nop;
                        returnInstruction.Operand = null;

                        var propertyChangedVariable = new VariableDefinition(assembly.MainModule.ImportReference(propertyChangedFieldType));
                        property.SetMethod.Body.Variables.Add(propertyChangedVariable);

                        var oldValueVariable = new VariableDefinition(objectType);
                        property.SetMethod.Body.Variables.Add(oldValueVariable);

                        Instruction jump1, jump2;

                        // Prepend:
                        // var propertyChanged = GetPropertyChanged();
                        // var oldValue = propertyChanged != null ? (object)Property : null;
                        property.SetMethod.Body.SimplifyMacros();
                        ilProcessor.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldarg_0));
                        ilProcessor.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Call, getPropertyChangedMethod));
                        ilProcessor.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Stloc, propertyChangedVariable));
                        ilProcessor.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldloc, propertyChangedVariable));
                        ilProcessor.InsertBefore(firstInstruction, jump1 = Instruction.Create(OpCodes.Brtrue, Instruction.Create(OpCodes.Nop)));
                        ilProcessor.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldnull));
                        ilProcessor.InsertBefore(firstInstruction, jump2 = Instruction.Create(OpCodes.Br, Instruction.Create(OpCodes.Nop)));
                        ilProcessor.InsertBefore(firstInstruction, (Instruction)(jump1.Operand = Instruction.Create(OpCodes.Ldarg_0)));
                        ilProcessor.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Call, propertyGetMethod));
                        if (property.PropertyType.IsValueType)
                            ilProcessor.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Box, property.PropertyType));
                        ilProcessor.InsertBefore(firstInstruction, (Instruction)(jump2.Operand = Instruction.Create(OpCodes.Nop)));
                        ilProcessor.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Stloc, oldValueVariable));

                        // Append:
                        // if (propertyChanged != null)
                        //     propertyChanged(this, new PropertyChangedExtendedEventArgs("Property", oldValue, Property));
                        ilProcessor.Append(Instruction.Create(OpCodes.Ldloc, propertyChangedVariable));
                        ilProcessor.Append(Instruction.Create(OpCodes.Ldnull));
                        ilProcessor.Append(Instruction.Create(OpCodes.Ceq));
                        ilProcessor.Append(Instruction.Create(OpCodes.Brtrue, newReturnInstruction));
                        ilProcessor.Append(Instruction.Create(OpCodes.Ldloc, propertyChangedVariable));
                        ilProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
                        ilProcessor.Append(Instruction.Create(OpCodes.Ldsfld, staticField));
                        ilProcessor.Append(Instruction.Create(OpCodes.Ldloc, oldValueVariable));
                        ilProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
                        ilProcessor.Append(Instruction.Create(OpCodes.Call, propertyGetMethod));
                        if (property.PropertyType.IsValueType)
                            ilProcessor.Append(Instruction.Create(OpCodes.Box, property.PropertyType));
                        ilProcessor.Append(Instruction.Create(OpCodes.Newobj, propertyChangedExtendedEventArgsConstructor));
                        ilProcessor.Append(Instruction.Create(OpCodes.Callvirt, propertyChangedInvoke));
                        ilProcessor.Append(Instruction.Create(OpCodes.Nop));
                        ilProcessor.Append(newReturnInstruction);
                        property.SetMethod.Body.OptimizeMacros();
                    }
                }
            }

            return modified;
        }

        // TODO MOVE THIS CODE TO FACTORIZE IT
        private static Instruction LocationToStloc(ILProcessor ilProcessor, int index)
        {
            Instruction stlocFixed;
            switch (index)
            {
                case 0:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_0);
                    break;
                case 1:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_1);
                    break;
                case 2:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_2);
                    break;
                case 3:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc_3);
                    break;
                default:
                    stlocFixed = ilProcessor.Create(OpCodes.Stloc, index);
                    break;
            }
            return stlocFixed;
        }

        // TODO MOVE THIS CODE TO FACTORIZE IT
        private static Instruction LocationToLdLoc(ILProcessor ilProcessor, int index)
        {
            Instruction ldlocFixed;
            switch (index)
            {
                case 0:
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_0);
                    break;
                case 1:
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_1);
                    break;
                case 2:
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_2);
                    break;
                case 3:
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc_3);
                    break;
                default:
                    ldlocFixed = ilProcessor.Create(OpCodes.Ldloc, index);
                    break;
            }
            return ldlocFixed;
        }
    }
}
