// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor
{
    /// <summary>
    /// Enumerates required subtypes the given serializer will use internally.
    /// </summary>
    public class CecilSerializerDependency : ICecilSerializerDependency
    {
        string genericSerializerTypeFullName;
        TypeReference genericSerializableType;

        /// <summary>
        /// Initializes a new instance of the <see cref="CecilSerializerDependency" /> class.
        /// It will enumerates T1, T2 from genericSerializerType{T1, T2}.
        /// </summary>
        /// <param name="genericSerializerType">Type of the generic serializer.</param>
        public CecilSerializerDependency(string genericSerializerTypeFullName)
        {
            if (genericSerializerTypeFullName == null)
                throw new ArgumentNullException("genericSerializerTypeFullName");

            this.genericSerializerTypeFullName = genericSerializerTypeFullName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CecilSerializerDependency" /> class.
        /// It will enumerates genericSerializableType{T1, T2} from genericSerializerType{T1, T2}.
        /// </summary>
        /// <param name="genericSerializerType">Type of the generic serializer.</param>
        /// <param name="genericSerializableType">Type of the generic serializable.</param>
        public CecilSerializerDependency(string genericSerializerTypeFullName, TypeReference genericSerializableType)
        {
            if (genericSerializerTypeFullName == null)
                throw new ArgumentNullException("genericSerializerTypeFullName");
            if (genericSerializableType == null)
                throw new ArgumentNullException("genericSerializableType");

            this.genericSerializerTypeFullName = genericSerializerTypeFullName;
            this.genericSerializableType = genericSerializableType;
        }

        public IEnumerable<TypeReference> EnumerateSubTypesFromSerializer(TypeReference serializerType)
        {
            // Check if serializer type name matches
            if (serializerType.IsGenericInstance && serializerType.GetElementType().FullName == genericSerializerTypeFullName)
            {
                if (genericSerializableType != null)
                    // Transforms genericSerializerType{T1, T2} into genericSerializableType{T1, T2}
                    return Enumerable.Repeat(genericSerializableType.MakeGenericType(((GenericInstanceType)serializerType).GenericArguments.ToArray()), 1);
                else
                    // Transforms genericSerializerType{T1, T2} into T1, T2
                    return ((GenericInstanceType)serializerType).GenericArguments;
            }

            return null;
        }
    }
}
