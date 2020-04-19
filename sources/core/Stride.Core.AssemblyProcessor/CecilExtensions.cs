// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Source: http://stackoverflow.com/questions/4968755/mono-cecil-call-generic-base-class-method-from-other-assembly
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Stride.Core.Serialization;
using Stride.Core.Storage;

namespace Stride.Core.AssemblyProcessor
{
    public static class CecilExtensions
    {
        // Not sure why Cecil made ContainsGenericParameter internal, but let's work around it by reflection.
        private static readonly MethodInfo containsGenericParameterGetMethod = typeof(MemberReference).GetProperty("ContainsGenericParameter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetMethod;

        public static bool IsResolvedValueType(this TypeReference type)
        {
            if (type.GetType() == typeof(TypeReference))
                type = type.Resolve();

            return type.IsValueType;
        }

        public static MethodDefinition GetEmptyConstructor(this TypeDefinition type, bool allowPrivate = false)
        {
            return type.Methods.FirstOrDefault(x => x.IsConstructor && (x.IsPublic || allowPrivate) && !x.IsStatic && x.Parameters.Count == 0);
        }

        public static ModuleDefinition GetStrideCoreModule(this AssemblyDefinition assembly)
        {
            var strideCoreAssembly = assembly.Name.Name == "Stride.Core"
                ? assembly
                : assembly.MainModule.AssemblyResolver.Resolve(new AssemblyNameReference("Stride.Core", null));
            var strideCoreModule = strideCoreAssembly.MainModule;
            return strideCoreModule;
        }

        public static void AddModuleInitializer(this MethodDefinition initializeMethod, int order = 0)
        {
            var assembly = initializeMethod.Module.Assembly;
            var strideCoreModule = GetStrideCoreModule(assembly);

            var moduleInitializerAttribute = strideCoreModule.GetType("Stride.Core.ModuleInitializerAttribute");
            var moduleInitializerCtor = moduleInitializerAttribute.GetConstructors().Single(x => !x.IsStatic && x.Parameters.Count == 1);
            initializeMethod.CustomAttributes.Add(
                new CustomAttribute(assembly.MainModule.ImportReference(moduleInitializerCtor))
                {
                    ConstructorArguments = { new CustomAttributeArgument(assembly.MainModule.TypeSystem.Int32, order) }
                });
        }

        public static TypeReference MakeGenericType(this TypeReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException();

            if (arguments.Length == 0)
                return self;

            var instance = new GenericInstanceType(self);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }

        public static FieldReference MakeGeneric(this FieldReference self, params TypeReference[] arguments)
        {
            if (arguments.Length == 0)
                return self;

            return new FieldReference(self.Name, self.FieldType, self.DeclaringType.MakeGenericType(arguments));
        }

        public static MethodReference MakeGeneric(this MethodReference self, params TypeReference[] arguments)
        {
            if (arguments.Length == 0)
                return self;

            var reference = new MethodReference(self.Name, self.ReturnType, self.DeclaringType.MakeGenericType(arguments))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention,
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            CopyGenericParameters(self, reference);

            return reference;
        }

        private static void CopyGenericParameters(MethodReference self, MethodReference reference)
        {
            foreach (var genericParameter in self.GenericParameters)
            {
                var genericParameterCopy = new GenericParameter(genericParameter.Name, reference)
                {
                    Attributes = genericParameter.Attributes,
                };
                reference.GenericParameters.Add(genericParameterCopy);
                foreach (var constraint in genericParameter.Constraints)
                    genericParameterCopy.Constraints.Add(constraint);
            }
        }

        public static MethodReference MakeGenericMethod(this MethodReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException();

            var method = new GenericInstanceMethod(self);
            foreach(var argument in arguments)
                method.GenericArguments.Add(argument);
            return method;
        }

        public static TypeDefinition GetTypeResolved(this ModuleDefinition moduleDefinition, string typeName)
        {
            foreach (var exportedType in moduleDefinition.ExportedTypes)
            {
                if (exportedType.FullName == typeName)
                {
                    var typeDefinition = exportedType.Resolve();
                    return typeDefinition;
                }
            }

            return moduleDefinition.GetType(typeName);
        }

        public static TypeDefinition GetTypeResolved(this ModuleDefinition moduleDefinition, string @namespace, string typeName)
        {
            foreach (var exportedType in moduleDefinition.ExportedTypes)
            {
                if (exportedType.Namespace == @namespace && exportedType.Name == typeName)
                {
                    var typeDefinition = exportedType.Resolve();
                    return typeDefinition;
                }
            }

            return moduleDefinition.GetType(@namespace, typeName);
        }

        /// <summary>
        /// Finds the corlib assembly which can be either mscorlib.dll or System.Runtime.dll depending on the .NET runtime environment.
        /// </summary>
        /// <param name="assembly">Assembly where System.Object is found.</param>
        /// <returns></returns>
        public static AssemblyDefinition FindCorlibAssembly(AssemblyDefinition assembly)
        {
            // Ask Cecil for the core library which will be either mscorlib or System.Runtime.
            AssemblyNameReference corlibReference = assembly.MainModule.TypeSystem.CoreLibrary as AssemblyNameReference;
            return assembly.MainModule.AssemblyResolver.Resolve(corlibReference);
        }

        /// <summary>
        /// Finds the assembly in which the generic collections are defined. This can be either in mscorlib.dll or in System.Collections.dll depending on the .NET runtime environment.
        /// </summary>
        /// <param name="assembly">Assembly where the generic collections are defined.</param>
        /// <returns></returns>
        public static AssemblyDefinition FindCollectionsAssembly(AssemblyDefinition assembly)
        {
            // Ask Cecil for the core library which will be either mscorlib or System.Runtime.
            var corlibReference = FindCorlibAssembly(assembly);

            if (corlibReference.Name.Name.ToLower() == "system.runtime")
            {
                // The core library is System.Runtime, so the collections assemblies are in System.Collections.dll.
                // First we look if it is not already referenced by `assembly' and if not, we made an explicit reference
                // to System.Collections.
                var collectionsAssembly = assembly.MainModule.AssemblyReferences.FirstOrDefault(ass => ass.Name.ToLower() == "system.collections");
                if (collectionsAssembly == null)
                {
                    collectionsAssembly = new AssemblyNameReference("System.Collections", new Version(4,0,0,0));
                }
                return assembly.MainModule.AssemblyResolver.Resolve(collectionsAssembly);
            }
            else
            {
                return corlibReference;
            }
        }

        /// <summary>
        /// Finds the assembly in which the generic collections are defined. This can be either in mscorlib.dll or in System.Collections.dll depending on the .NET runtime environment.
        /// </summary>
        /// <param name="assembly">Assembly where the generic collections are defined.</param>
        /// <returns></returns>
        public static AssemblyDefinition FindReflectionAssembly(AssemblyDefinition assembly)
        {
            // Ask Cecil for the core library which will be either mscorlib or System.Runtime.
            var corlibReference = FindCorlibAssembly(assembly);

            if (corlibReference.Name.Name.ToLower() == "system.runtime")
            {
                // The core library is System.Runtime, so the collections assemblies are in System.Collections.dll.
                // First we look if it is not already referenced by `assembly' and if not, we made an explicit reference
                // to System.Collections.
                var collectionsAssembly = assembly.MainModule.AssemblyReferences.FirstOrDefault(ass => ass.Name.ToLower() == "system.reflection");
                if (collectionsAssembly == null)
                {
                    collectionsAssembly = new AssemblyNameReference("System.Reflection", new Version(4, 0, 0, 0));
                }
                return assembly.MainModule.AssemblyResolver.Resolve(collectionsAssembly);
            }
            else
            {
                return corlibReference;
            }
        }

        /// <summary>
        /// Get AssemblyProcessorProgram Files x86
        /// </summary>
        /// <returns></returns>
        public static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        public static GenericInstanceType ChangeGenericInstanceType(this GenericInstanceType type, TypeReference elementType, IEnumerable<TypeReference> genericArguments)
        {
            if (elementType != type.ElementType || genericArguments != type.GenericArguments)
            {
                var result = new GenericInstanceType(elementType);
                foreach (var genericArgument in genericArguments)
                    result.GenericArguments.Add(genericArgument);
                if (type.HasGenericParameters)
                    SetGenericParameters(result, type.GenericParameters);
                return result;
            }
            return type;
        }

        public static ArrayType ChangeArrayType(this ArrayType type, TypeReference elementType, int rank)
        {
            if (elementType != type.ElementType || rank != type.Rank)
            {
                var result = new ArrayType(elementType, rank);
                if (type.HasGenericParameters)
                    SetGenericParameters(result, type.GenericParameters);
                return result;
            }
            return type;
        }

        public static PointerType ChangePointerType(this PointerType type, TypeReference elementType)
        {
            if (elementType != type.ElementType)
            {
                var result = new PointerType(elementType);
                if (type.HasGenericParameters)
                    SetGenericParameters(result, type.GenericParameters);
                return result;
            }
            return type;
        }

        public static PinnedType ChangePinnedType(this PinnedType type, TypeReference elementType)
        {
            if (elementType != type.ElementType)
            {
                var result = new PinnedType(elementType);
                if (type.HasGenericParameters)
                    SetGenericParameters(result, type.GenericParameters);
                return result;
            }
            return type;
        }

        public static TypeReference ChangeGenericParameters(this TypeReference type, IEnumerable<GenericParameter> genericParameters)
        {
            if (type.GenericParameters == genericParameters)
                return type;

            TypeReference result;
            var arrayType = type as ArrayType;
            if (arrayType != null)
            {
                result = new ArrayType(arrayType.ElementType, arrayType.Rank);
            }
            else
            {
                var genericInstanceType = type as GenericInstanceType;
                if (genericInstanceType != null)
                {
                    result = new GenericInstanceType(genericInstanceType.ElementType);
                }
                else if (type.GetType() == typeof(TypeReference).GetType())
                {
                    result = new TypeReference(type.Namespace, type.Name, type.Module, type.Scope, type.IsValueType);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            SetGenericParameters(result, genericParameters);

            return result;
        }

        /// <summary>
        /// Sometimes, TypeReference.IsValueType is not properly set (since it needs to load dependent assembly).
        /// THis do so when necessary.
        /// </summary>
        /// <param name="typeReference"></param>
        /// <returns></returns>
        public static TypeReference FixupValueType(this TypeReference typeReference)
        {
            return FixupValueTypeVisitor.Default.VisitDynamic(typeReference);
        }

        private static void SetGenericParameters(TypeReference result, IEnumerable<GenericParameter> genericParameters)
        {
            foreach (var genericParameter in genericParameters)
                result.GenericParameters.Add(genericParameter);
        }

        public static string GenerateGenerics(this TypeReference type, bool empty = false)
        {
            var genericInstanceType = type as GenericInstanceType;
            if (!type.HasGenericParameters && genericInstanceType == null)
                return string.Empty;

            var result = new StringBuilder();

            // Try to process generic instantiations
            if (genericInstanceType != null)
            {
                result.Append("<");

                bool first = true;
                foreach (var genericArgument in genericInstanceType.GenericArguments)
                {
                    if (!first)
                        result.Append(",");
                    first = false;
                    if (!empty)
                        result.Append(ConvertCSharp(genericArgument, empty));
                }

                result.Append(">");

                return result.ToString();
            }

            if (type.HasGenericParameters)
            {
                result.Append("<");

                bool first = true;
                foreach (var genericParameter in type.GenericParameters)
                {
                    if (!first)
                        result.Append(",");
                    first = false;
                    if (!empty)
                        result.Append(ConvertCSharp(genericParameter, empty));
                }

                result.Append(">");

                return result.ToString();
            }

            return result.ToString();
        }

        public unsafe static string ConvertTypeId(this TypeReference type)
        {
            var typeName = type.ConvertCSharp(false);
            var typeId = ObjectId.FromBytes(Encoding.UTF8.GetBytes(typeName));

            var typeIdHash = (uint*)&typeId;
            return string.Format("new {0}(0x{1:x8}, 0x{2:x8}, 0x{3:x8}, 0x{4:x8})", typeof(ObjectId).FullName, typeIdHash[0], typeIdHash[1], typeIdHash[2], typeIdHash[3]);
        }

        /// <summary>
        /// Generates type name valid to use from C# source file.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="empty"></param>
        /// <returns></returns>
        public static string ConvertCSharp(this TypeReference type, bool empty = false)
        {
            // Try to process arrays
            var arrayType = type as ArrayType;
            if (arrayType != null)
            {
                return ConvertCSharp(arrayType.ElementType, empty) + "[]";
            }

            // Remove the `X at end of generic definition.
            var typeName = type.GetElementType().FullName;
            var genericSeparatorIndex = typeName.LastIndexOf('`');
            if (genericSeparatorIndex != -1)
                typeName = typeName.Substring(0, genericSeparatorIndex);

            // Replace / into . (nested types)
            typeName = typeName.Replace('/', '.');

            // Try to process generic instantiations
            var genericInstanceType = type as GenericInstanceType;
            if (genericInstanceType != null)
            {
                var result = new StringBuilder();

                // Use ElementType so that we have only the name without the <> part.
                result.Append(typeName);
                result.Append("<");

                bool first = true;
                foreach (var genericArgument in genericInstanceType.GenericArguments)
                {
                    if (!first)
                        result.Append(",");
                    first = false;
                    if (!empty)
                        result.Append(ConvertCSharp(genericArgument, empty));
                }

                result.Append(">");

                return result.ToString();
            }

            if (type.HasGenericParameters)
            {
                var result = new StringBuilder();

                // Use ElementType so that we have only the name without the <> part.
                result.Append(typeName);
                result.Append("<");

                bool first = true;
                foreach (var genericParameter in type.GenericParameters)
                {
                    if (!first)
                        result.Append(",");
                    first = false;
                    if (!empty)
                        result.Append(ConvertCSharp(genericParameter, empty));
                }

                result.Append(">");

                return result.ToString();
            }

            return typeName;
        }

        /// <summary>
        /// Generates the Mono.Cecil TypeReference from its .NET <see cref="Type"/> counterpart.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="assemblyResolver">The assembly resolver.</param>
        /// <returns></returns>
        public static TypeReference GenerateTypeCecil(this Type type, BaseAssemblyResolver assemblyResolver)
        {
            var assemblyDefinition = assemblyResolver.Resolve(AssemblyNameReference.Parse(type.Assembly.FullName));
            TypeReference typeReference;

            if (type.IsNested)
            {
                var declaringType = GenerateTypeCecil(type.DeclaringType, assemblyResolver);
                typeReference = declaringType.Resolve().NestedTypes.FirstOrDefault(x => x.Name == type.Name);
            }
            else if (type.IsArray)
            {
                var elementType = GenerateTypeCecil(type.GetElementType(), assemblyResolver);
                typeReference = new ArrayType(elementType, type.GetArrayRank());
            }
            else
            {
                typeReference = assemblyDefinition.MainModule.GetTypeResolved(type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName);
            }

            if (typeReference == null)
                throw new InvalidOperationException("Could not resolve cecil type.");

            if (type.IsGenericType)
            {
                var genericInstanceType = new GenericInstanceType(typeReference);
                foreach (var argType in type.GetGenericArguments())
                {
                    TypeReference argTypeReference;
                    if (argType.IsGenericParameter)
                    {
                        argTypeReference = new GenericParameter(argType.Name, typeReference);
                    }
                    else
                    {
                        argTypeReference = GenerateTypeCecil(argType, assemblyResolver);
                    }
                    genericInstanceType.GenericArguments.Add(argTypeReference);
                }

                typeReference = genericInstanceType;
            }

            return typeReference;
        }

        public static bool ContainsGenericParameter(this MemberReference memberReference)
        {
            return (bool)containsGenericParameterGetMethod.Invoke(memberReference, null);
        }

        /// <summary>
        /// Generates type name similar to Type.AssemblyQualifiedName.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ConvertAssemblyQualifiedName(this TypeReference type)
        {
            var result = new StringBuilder(256);
            ConvertAssemblyQualifiedName(type, result);
            return result.ToString();
        }

        private static void ConvertAssemblyQualifiedName(this TypeReference type, StringBuilder result)
        {
            int start, end;

            var arrayType = type as ArrayType;
            if (arrayType != null)
            {
                // If it's an array, process element type, and add [] after
                type = arrayType.ElementType;
            }

            // Add FUllName from GetElementType() (remove generics etc...)
            start = result.Length;
            result.Append(type.GetElementType().FullName);
            end = result.Length;

            // Replace / into + (nested types)
            result = result.Replace('/', '+', start, end);

            // Try to process generic instantiations
            var genericInstanceType = type as GenericInstanceType;
            if (genericInstanceType != null)
            {
                // Ideally we would like to have access to Mono.Cecil TypeReference.ContainsGenericParameter, but it's internal.
                // This doesn't cover every case but hopefully this should be enough for serialization
                bool containsGenericParameter = false;
                foreach (var genericArgument in genericInstanceType.GenericArguments)
                {
                    if (genericArgument.IsGenericParameter)
                        containsGenericParameter = true;
                }

                if (!containsGenericParameter)
                {
                    // Use ElementType so that we have only the name without the <> part.
                    result.Append('[');

                    bool first = true;
                    foreach (var genericArgument in genericInstanceType.GenericArguments)
                    {
                        if (!first)
                            result.Append(",");
                        result.Append('[');
                        first = false;
                        result.Append(ConvertAssemblyQualifiedName(genericArgument));
                        result.Append(']');
                    }

                    result.Append(']');
                }
            }

            // Try to process arrays
            if (arrayType != null)
            {
                result.Append('[');
                if (arrayType.Rank > 1)
                    result.Append(',', arrayType.Rank - 1);
                result.Append(']');
            }

            result.Append(", ");
            start = result.Length;
            result.Append(type.Module.Assembly.FullName);
            end = result.Length;

#if STRIDE_PLATFORM_MONO_MOBILE
            // Xamarin iOS and Android remap some assemblies
            const string oldTypeEnding = "2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
            const string newTypeEnding = "4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            result = result.Replace(oldTypeEnding, newTypeEnding, start, end);
#endif
        }

        public static void AddRange<T>(this ICollection<T> list, IEnumerable<T> items)
        {
            var l = list as List<T>;
            if (l != null)
            {
                l.AddRange(items);
            }
            else
            {
                foreach (var item in items)
                {
                    list.Add(item);
                }
            }
        }

        public static void InflateGenericType(TypeDefinition genericType, TypeDefinition inflatedType, params TypeReference[] genericTypes)
        {
            // Base type
            var genericMapping = new Dictionary<TypeReference, TypeReference>();
            for (int i = 0; i < genericTypes.Length; ++i)
            {
                genericMapping.Add(genericType.GenericParameters[i], genericTypes[i]);
            }

            var resolveGenericsVisitor = new ResolveGenericsVisitor(genericMapping);
            inflatedType.BaseType = inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(genericType.BaseType));

            // Some stuff are not handled yet
            if (genericType.HasNestedTypes)
            {
                throw new NotImplementedException();
            }

            foreach (var field in genericType.Fields)
            {
                var clonedField = new FieldDefinition(field.Name, field.Attributes, inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(field.FieldType)));
                inflatedType.Fields.Add(clonedField);
            }

            foreach (var property in genericType.Properties)
            {
                if (property.HasParameters)
                    throw new NotImplementedException();

                var clonedProperty = new PropertyDefinition(property.Name, property.Attributes, inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(property.PropertyType)))
                {
                    HasThis = property.HasThis,
                    GetMethod = property.GetMethod != null ? InflateMethod(inflatedType, property.GetMethod, resolveGenericsVisitor) : null,
                    SetMethod = property.SetMethod != null ? InflateMethod(inflatedType, property.GetMethod, resolveGenericsVisitor) : null,
                };

                inflatedType.Properties.Add(clonedProperty);
            }

            // Clone methods
            foreach (var method in genericType.Methods)
            {
                var clonedMethod = InflateMethod(inflatedType, method, resolveGenericsVisitor);
                inflatedType.Methods.Add(clonedMethod);
            }
        }

        private static MethodDefinition InflateMethod(TypeDefinition inflatedType, MethodDefinition method, ResolveGenericsVisitor resolveGenericsVisitor)
        {
            var clonedMethod = new MethodDefinition(method.Name, method.Attributes, inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(method.ReturnType)));
            clonedMethod.Parameters.AddRange(
                method.Parameters.Select(x => new ParameterDefinition(x.Name, x.Attributes, inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(x.ParameterType)))));

            if (method.Body != null)
            {
                clonedMethod.Body.Variables.AddRange(
                    method.Body.Variables.Select(x => new VariableDefinition(inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(x.VariableType)))));


                clonedMethod.Body.InitLocals = method.Body.InitLocals;

                var mappedInstructions = new Dictionary<Instruction, Instruction>();
                foreach (var instruction in method.Body.Instructions)
                {
                    // Create nop instructions to start with (if we use actual opcode, it would do an operand check)
                    var mappedInstruction = Instruction.Create(OpCodes.Nop);
                    mappedInstruction.OpCode = instruction.OpCode;
                    mappedInstruction.Operand = instruction.Operand;
                    mappedInstructions[instruction] = mappedInstruction;
                }

                foreach (var instruction in method.Body.Instructions)
                {
                    // Fix operand
                    var mappedInstruction = mappedInstructions[instruction];
                    if (mappedInstruction.Operand is Instruction)
                    {
                        mappedInstruction.Operand = mappedInstructions[(Instruction)instruction.Operand];
                    }
                    else if (mappedInstruction.Operand is ParameterDefinition)
                    {
                        var parameterIndex = method.Parameters.IndexOf((ParameterDefinition)instruction.Operand);
                        mappedInstruction.Operand = clonedMethod.Parameters[parameterIndex];
                    }
                    else if (mappedInstruction.Operand is VariableDefinition)
                    {
                        var variableIndex = method.Body.Variables.IndexOf((VariableDefinition)instruction.Operand);
                        mappedInstruction.Operand = clonedMethod.Body.Variables[variableIndex];
                    }
                    else if (mappedInstruction.Operand is TypeReference)
                    {
                        var newTypeReference = resolveGenericsVisitor.VisitDynamic((TypeReference)mappedInstruction.Operand);
                        newTypeReference = inflatedType.Module.ImportReference(newTypeReference);
                        mappedInstruction.Operand = newTypeReference;
                    }
                    else if (mappedInstruction.Operand is FieldReference)
                    {
                        var fieldReference = (FieldReference)mappedInstruction.Operand;
                        var newFieldReference = new FieldReference(fieldReference.Name,
                            inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(fieldReference.FieldType)),
                            inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(fieldReference.DeclaringType)));
                        mappedInstruction.Operand = newFieldReference;
                    }
                    else if (mappedInstruction.Operand is MethodReference)
                    {
                        var methodReference = (MethodReference)mappedInstruction.Operand;

                        var genericInstanceMethod = methodReference as GenericInstanceMethod;
                        if (genericInstanceMethod != null)
                        {
                            methodReference = genericInstanceMethod.ElementMethod;
                        }

                        methodReference = methodReference.GetElementMethod();
                        var newMethodReference = new MethodReference(methodReference.Name,
                            inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(methodReference.ReturnType)),
                            inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(methodReference.DeclaringType)))
                        {
                            HasThis = methodReference.HasThis,
                            ExplicitThis = methodReference.ExplicitThis,
                            CallingConvention = methodReference.CallingConvention,
                        };

                        foreach (var parameter in methodReference.Parameters)
                            newMethodReference.Parameters.Add(new ParameterDefinition(inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(parameter.ParameterType))));

                        if (methodReference.HasGenericParameters)
                        {
                            CopyGenericParameters(methodReference, newMethodReference);
                        }

                        if (genericInstanceMethod != null)
                        {
                            newMethodReference = newMethodReference.MakeGenericMethod(genericInstanceMethod.GenericArguments.Select(x => inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(x))).ToArray());
                        }

                        mappedInstruction.Operand = newMethodReference;
                    }
                    else if (mappedInstruction.Operand is Mono.Cecil.CallSite)
                    {
                        var callSite = (Mono.Cecil.CallSite)mappedInstruction.Operand;
                        var newCallSite = new Mono.Cecil.CallSite(inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(callSite.ReturnType)))
                        {
                            HasThis = callSite.HasThis,
                            ExplicitThis = callSite.ExplicitThis,
                            CallingConvention = callSite.CallingConvention,
                        };

                        foreach (var parameter in callSite.Parameters)
                            newCallSite.Parameters.Add(new ParameterDefinition(inflatedType.Module.ImportReference(resolveGenericsVisitor.VisitDynamic(parameter.ParameterType))));

                        mappedInstruction.Operand = newCallSite;
                    }
                    else if (mappedInstruction.Operand is Instruction[])
                    {
                        // Not used in UpdatableProperty<T>
                        throw new NotImplementedException();
                    }
                }

                clonedMethod.Body.Instructions.AddRange(method.Body.Instructions.Select(x => mappedInstructions[x]));
            }
            return clonedMethod;
        }
    }
}
