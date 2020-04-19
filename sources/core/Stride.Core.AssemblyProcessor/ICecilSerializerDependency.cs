// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor
{
    /// <summary>
    /// Enumerates required subtypes the given serializer will use internally.
    /// This is useful for generation of serialization assembly, when AOT is performed (all generic serializers must be available).
    /// </summary>
    public interface ICecilSerializerDependency
    {
        /// <summary>
        /// Enumerates the types this serializer requires.
        /// </summary>
        /// <param name="serializerType">Type of the serializer.</param>
        /// <returns></returns>
        IEnumerable<TypeReference> EnumerateSubTypesFromSerializer(TypeReference serializerType);
    }
}
