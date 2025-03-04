// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor;

/// <summary>
/// Enumerates required subtypes the given serializer will use internally.
/// </summary>
public class CecilSerializerDependency : ICecilSerializerDependency
{
    readonly string genericSerializerTypeFullName;
    readonly TypeReference? genericSerializableType;

    /// <summary>
    /// Initializes a new instance of the <see cref="CecilSerializerDependency" /> class.
    /// It will enumerates T1, T2 from genericSerializerType{T1, T2}.
    /// </summary>
    /// <param name="genericSerializerTypeFullName">Type of the generic serializer.</param>
    public CecilSerializerDependency(string genericSerializerTypeFullName)
    {
        this.genericSerializerTypeFullName = genericSerializerTypeFullName ?? throw new ArgumentNullException(nameof(genericSerializerTypeFullName));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CecilSerializerDependency" /> class.
    /// It will enumerates genericSerializableType{T1, T2} from genericSerializerType{T1, T2}.
    /// </summary>
    /// <param name="genericSerializerTypeFullName">Type of the generic serializer.</param>
    /// <param name="genericSerializableType">Type of the generic serializable.</param>
    public CecilSerializerDependency(string genericSerializerTypeFullName, TypeReference genericSerializableType)
    {
        this.genericSerializerTypeFullName = genericSerializerTypeFullName ?? throw new ArgumentNullException(nameof(genericSerializerTypeFullName));
        this.genericSerializableType = genericSerializableType ?? throw new ArgumentNullException(nameof(genericSerializableType));
    }

    public IEnumerable<TypeReference> EnumerateSubTypesFromSerializer(TypeReference serializerType)
    {
        // Check if serializer type name matches
        if (serializerType.IsGenericInstance && serializerType.GetElementType().FullName == genericSerializerTypeFullName)
        {
            if (genericSerializableType != null)
            {
                // Transforms genericSerializerType{T1, T2} into genericSerializableType{T1, T2}
                return Enumerable.Repeat(genericSerializableType.MakeGenericType([.. ((GenericInstanceType)serializerType).GenericArguments]), 1);
            }
            else
            {
                // Transforms genericSerializerType{T1, T2} into T1, T2
                return ((GenericInstanceType)serializerType).GenericArguments;
            }
        }

        return [];
    }
}
