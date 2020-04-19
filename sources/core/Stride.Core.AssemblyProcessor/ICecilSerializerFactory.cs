// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor
{
    /// <summary>
    /// Gives the required generic serializer for a given type.
    /// This is useful for generation of serialization assembly, when AOT is performed (all generic serializers must be available).
    /// </summary>
    public interface ICecilSerializerFactory
    {
        /// <summary>
        /// Gets the serializer type from a given object type.
        /// </summary>
        /// <param name="objectType">Type of the object to serialize.</param>
        /// <returns></returns>
        TypeReference GetSerializer(TypeReference objectType);
    }
}
