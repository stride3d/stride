// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name
using System;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization;

namespace Xenko.Rendering
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
                obj = (ValueParameterKey<T>)ParameterKeys.FindByName(parameterName);

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
                obj = (ObjectParameterKey<T>)ParameterKeys.FindByName(parameterName);

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
                obj = (PermutationParameterKey<T>)ParameterKeys.FindByName(parameterName);

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
