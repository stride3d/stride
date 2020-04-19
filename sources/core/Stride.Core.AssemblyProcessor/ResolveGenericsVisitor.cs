// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor
{
    /// <summary>
    /// Transform open generic types to closed instantiation using context information.
    /// See <see cref="Process"/> for more details.
    /// </summary>
    class ResolveGenericsVisitor : CecilTypeReferenceVisitor
    {
        private Dictionary<TypeReference, TypeReference> genericTypeMapping;

        public ResolveGenericsVisitor(Dictionary<TypeReference, TypeReference> genericTypeMapping)
        {
            this.genericTypeMapping = genericTypeMapping;
        }

        /// <summary>
        /// Transform open generic types to closed instantiation using context information.
        /// As an example, if B{T} inherits from A{T}, running it with B{C} as context and A{B.T} as type, ti will return A{C}.
        /// </summary>
        public static TypeReference Process(TypeReference context, TypeReference type)
        {
            if (type == null)
                return null;

            var parentContext = context;
            GenericInstanceType genericInstanceTypeContext = null;
            while (parentContext != null)
            {
                genericInstanceTypeContext = parentContext as GenericInstanceType;
                if (genericInstanceTypeContext != null)
                    break;

                parentContext = parentContext.Resolve().BaseType;
            }
            if (genericInstanceTypeContext == null || genericInstanceTypeContext.ContainsGenericParameter())
                return type;

            // Build dictionary that will map generic type to their real implementation type
            var genericTypeMapping = new Dictionary<TypeReference, TypeReference>();
            while (parentContext != null)
            {
                var resolvedType = parentContext.Resolve();
                for (int i = 0; i < resolvedType.GenericParameters.Count; ++i)
                {
                    var genericParameter = parentContext.GetElementType().Resolve().GenericParameters[i];
                    genericTypeMapping.Add(genericParameter, genericInstanceTypeContext.GenericArguments[i]);
                }
                parentContext = parentContext.Resolve().BaseType;
                if (parentContext is GenericInstanceType)
                    genericInstanceTypeContext = parentContext as GenericInstanceType;
            }

            var visitor = new ResolveGenericsVisitor(genericTypeMapping);
            var result = visitor.VisitDynamic(type);

            // Make sure type is closed now
            if (result.ContainsGenericParameter())
                throw new InvalidOperationException("Unsupported generic resolution.");

            return result;
        }

        public override TypeReference Visit(GenericParameter type)
        {
            TypeReference result;
            TypeReference typeParent = type;

            while (genericTypeMapping.TryGetValue(typeParent, out result))
                typeParent = result;

            if (typeParent != type)
                return typeParent;

            return base.Visit(type);
        }
    }
}
