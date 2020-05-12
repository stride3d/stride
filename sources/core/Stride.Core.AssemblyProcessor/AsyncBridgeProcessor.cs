// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace Stride.Core.AssemblyProcessor
{
    /// <summary>
    /// Makes the assembly use AsyncBridge instead of mscorlib for async.
    /// </summary>
    internal class AsyncBridgeProcessor : IAssemblyDefinitionProcessor
    {
        private AssemblyDefinition assembly;

        private AssemblyDefinition asyncBridgeAssembly;

        public bool Process(AssemblyProcessorContext context)
        {
            this.assembly = context.Assembly;
            asyncBridgeAssembly = context.AssemblyResolver.Resolve(new AssemblyNameReference("AsyncBridge", null));

            assembly.MainModule.AssemblyReferences.Add(asyncBridgeAssembly.Name);

            foreach (var type in assembly.MainModule.Types)
            {
                ProcessType(type);
            }

            return true;
        }

        private TypeReference ProcessTypeReference(TypeReference type, IGenericParameterProvider owner)
        {
            if (type == null)
                return null;

            // ref types
            if (type is ByReferenceType)
            {
                var elementType = ProcessTypeReference(type.GetElementType(), owner);
                if (elementType != type.GetElementType())
                    type = new ByReferenceType(elementType);
                return type;
            }

            // Generic MVar/Var
            if (type is GenericParameter)
            {
                var genericParameter = (GenericParameter)type;
                if ((genericParameter.MetadataType == MetadataType.MVar
                    || genericParameter.MetadataType == MetadataType.Var) && genericParameter.Owner is MethodReference)
                {
                    if (genericParameter.Owner != null && owner != null)
                    {
                        return owner.GenericParameters.Concat(genericParameter.Owner.GenericParameters).First(x => x.Name == type.Name);
                    }
                }
                return type;
            }

            // Is it an async-related type?
            var asyncBridgeType = asyncBridgeAssembly.MainModule.GetTypeResolved(type.Namespace, type.Name);
            if (asyncBridgeType == null)
                return type;

            // First work on inner TypeReference if there is a GenericInstanceType around it.
            var genericInstanceType = type as GenericInstanceType;
            if (genericInstanceType != null)
            {
                type = genericInstanceType.ElementType;
            }

            var newType = assembly.MainModule.ImportReference(new TypeReference(type.Namespace, type.Name, asyncBridgeAssembly.MainModule, asyncBridgeAssembly.Name, type.IsValueType));

            for (int i = 0; i < type.GenericParameters.Count; ++i)
            {
                newType.GenericParameters.Add(new GenericParameter(type.GenericParameters[i].Name, newType));
                foreach (var constraint in type.GenericParameters[i].Constraints)
                    newType.GenericParameters[i].Constraints.Add(new GenericParameterConstraint(ProcessTypeReference(constraint.ConstraintType, newType)));
            }

            if (genericInstanceType != null)
            {
                var newGenericType = new GenericInstanceType(newType);
                foreach (var genericArgument in genericInstanceType.GenericArguments)
                {
                    newGenericType.GenericArguments.Add(ProcessTypeReference(genericArgument, newGenericType));
                }
                newType = newGenericType;
            }

            return newType;
        }

        private FieldReference ProcessFieldReference(FieldReference field)
        {
            if (field == null)
                return null;

            field.FieldType = ProcessTypeReference(field.FieldType, field.DeclaringType);
            return field;
        }

        private MethodReference ProcessMethodReference(MethodReference method)
        {
            if (method == null)
                return null;

            var declaringType = ProcessTypeReference(method.DeclaringType, null);
            if (declaringType == method.DeclaringType)
            {
                return method;
            }

            var genericInstanceMethod = method as GenericInstanceMethod;
            if (genericInstanceMethod != null)
            {
                method = genericInstanceMethod.ElementMethod;
            }
            
            var newMethod = new MethodReference(method.Name, assembly.MainModule.TypeSystem.Void, declaringType);
            newMethod.HasThis = method.HasThis;
            newMethod.ExplicitThis = method.ExplicitThis;
            newMethod.CallingConvention = method.CallingConvention;
            newMethod.MethodReturnType = method.MethodReturnType;
            newMethod.ReturnType = ProcessTypeReference(method.ReturnType, newMethod);

            for (int i = 0; i < method.GenericParameters.Count; ++i)
            {
                newMethod.GenericParameters.Add(new GenericParameter(method.GenericParameters[i].Name, newMethod));
                foreach (var constraint in method.GenericParameters[i].Constraints)
                    newMethod.GenericParameters[i].Constraints.Add(new GenericParameterConstraint(ProcessTypeReference(constraint.ConstraintType, newMethod)));
            }
            
            for (int i = 0; i < method.Parameters.Count; ++i)
            {
                var parameterDefinition = new ParameterDefinition(method.Parameters[i].Name, method.Parameters[i].Attributes, ProcessTypeReference(method.Parameters[i].ParameterType, newMethod));
                newMethod.Parameters.Add(parameterDefinition);
            }

            if (genericInstanceMethod != null)
            {
                var newGenericMethod = new GenericInstanceMethod(newMethod);
                foreach (var genericArgument in genericInstanceMethod.GenericArguments)
                {
                    newGenericMethod.GenericArguments.Add(ProcessTypeReference(genericArgument, newMethod));
                }
                newMethod = newGenericMethod;
            }

            return newMethod;
        }

        private void ProcessType(TypeDefinition type)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                ProcessType(nestedType);
            }

            type.BaseType = ProcessTypeReference(type.BaseType, null);

            for (int i = 0; i < type.Interfaces.Count; ++i)
            {
                type.Interfaces[i].InterfaceType = ProcessTypeReference(type.Interfaces[i].InterfaceType, null);
            }

            foreach (var field in type.Fields)
            {
                field.FieldType = ProcessTypeReference(field.FieldType, null);
            }

            foreach (var property in type.Properties)
            {
                property.PropertyType = ProcessTypeReference(property.PropertyType, null);
            }

            foreach (var method in type.Methods)
            {
                ProcessMethod(method);
            }

            foreach (var attribute in type.CustomAttributes)
            {
                attribute.Constructor = ProcessMethodReference(attribute.Constructor);
            }
        }

        private void ProcessMethod(MethodDefinition method)
        {
            for (int i = 0; i < method.Overrides.Count; ++i)
            {
                method.Overrides[i] = ProcessMethodReference(method.Overrides[i]);
            }

            foreach (var parameter in method.Parameters)
            {
                parameter.ParameterType = ProcessTypeReference(parameter.ParameterType, null);
            }

            foreach (var attribute in method.CustomAttributes)
            {
                attribute.Constructor = ProcessMethodReference(attribute.Constructor);
            }

            var methodBody = method.Body;

            if (methodBody == null)
                return;

            foreach (var variable in methodBody.Variables)
            {
                variable.VariableType = ProcessTypeReference(variable.VariableType, null);
            }

            foreach (var instruction in methodBody.Instructions)
            {
                if (instruction.Operand == null)
                    continue;

                if (instruction.Operand is TypeReference)
                    instruction.Operand = ProcessTypeReference((TypeReference)instruction.Operand, null);
                else if (instruction.Operand is MethodReference)
                    instruction.Operand = ProcessMethodReference((MethodReference)instruction.Operand);
                else if (instruction.Operand is FieldReference)
                    instruction.Operand = ProcessFieldReference((FieldReference)instruction.Operand);
            }
        }
    }
}
