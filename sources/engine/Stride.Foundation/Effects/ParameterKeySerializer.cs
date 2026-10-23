// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name
using System;
using Stride.Core.Reflection;
using Stride.Core.Serialization;

namespace Stride.Rendering
{
    public class ValueParameterKeySerializer<T> : DataSerializer<ValueParameterKey<T>> where T : struct
    {
        public override void Serialize(ref ValueParameterKey<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Name);
                stream.Write(obj.Length);
            }
            else
            {
                var parameterName = stream.ReadString();
                var parameterLength = stream.ReadInt32();
                var newObj = ParameterKeys.FindByName(parameterName);
                if (newObj is not ValueParameterKey<T> valueKey)
                {
                    throw new InvalidCastException($"Parameter key '{parameterName}' is not of type '{typeof(ValueParameterKey<T>)}', but instead is of type '{newObj?.GetType()}' with owner '{newObj?.OwnerType}'.");
                }
                obj = valueKey;

                // If parameter could not be found, create one matching this type.
                if (obj == null)
                {
                    var metadata = new ParameterKeyValueMetadata<T>();
                    obj = new ValueParameterKey<T>(parameterName, parameterLength, metadata);
                    ParameterKeys.Merge(obj, null, parameterName);
                }
            }
        }
    }

    public class ObjectParameterKeySerializer<T> : DataSerializer<ObjectParameterKey<T>>
    {
        public override void Serialize(ref ObjectParameterKey<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Name);
                stream.Write(obj.Length);
            }
            else
            {
                var parameterName = stream.ReadString();
                var parameterLength = stream.ReadInt32();
                var newObj = ParameterKeys.FindByName(parameterName);
                if (newObj is not ObjectParameterKey<T> objectKey)
                {
                    throw new InvalidCastException($"Parameter key '{parameterName}' is not of type '{typeof(ObjectParameterKey<T>)}', but instead is of type '{newObj?.GetType()}' with owner '{newObj?.OwnerType}'.");
                }
                obj = objectKey;

                // If parameter could not be found, create one matching this type.
                if (obj == null)
                {
                    var metadata = new ParameterKeyValueMetadata<T>();
                    obj = new ObjectParameterKey<T>(parameterName, parameterLength, metadata);
                    ParameterKeys.Merge(obj, null, parameterName);
                }
            }
        }
    }

    public class PermutationParameterKeySerializer<T> : DataSerializer<PermutationParameterKey<T>>
    {
        public override void Serialize(ref PermutationParameterKey<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Name);
                stream.Write(obj.Length);
            }
            else
            {
                var parameterName = stream.ReadString();
                var parameterLength = stream.ReadInt32();
                var newObj = ParameterKeys.FindByName(parameterName);
                if (newObj is not PermutationParameterKey<T> permutationKey)
                {
                    throw new InvalidCastException($"Parameter key '{parameterName}' is not of type '{typeof(PermutationParameterKey<T>)}', but instead is of type '{newObj?.GetType()}' with owner '{newObj?.OwnerType}'.");
                }
                obj = permutationKey;

                // If parameter could not be found, create one matching this type.
                if (obj == null)
                {
                    var metadata = new ParameterKeyValueMetadata<T>();
                    obj = new PermutationParameterKey<T>(parameterName, parameterLength, metadata);
                    ParameterKeys.Merge(obj, null, parameterName);
                }
            }
        }
    }
}
