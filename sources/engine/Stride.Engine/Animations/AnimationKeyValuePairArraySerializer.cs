// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Animations
{
    public class AnimationKeyValuePairArraySerializer<T> : DataSerializer<AnimationKeyValuePair<T>[]> where T : struct
    {
        private DataSerializer<AnimationKeyValuePair<T>> itemDataSerializer;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            itemDataSerializer = MemberSerializer<AnimationKeyValuePair<T>>.Create(serializerSelector);
        }

        public override void PreSerialize(ref AnimationKeyValuePair<T>[] obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Length);
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                int length = stream.ReadInt32();
                obj = new AnimationKeyValuePair<T>[length];
            }
        }

        public unsafe override void Serialize(ref AnimationKeyValuePair<T>[] obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var rawData = stream.ReadBytes(Unsafe.SizeOf<AnimationKeyValuePair<T>>() * obj.Length);
                var destination = MemoryMarshal.AsBytes(obj.AsSpan());
                rawData.AsSpan().CopyTo(destination);
            }
            else if (mode == ArchiveMode.Serialize)
            {
                int count = obj.Length;
                for (int i = 0; i < count; ++i)
                {
                    itemDataSerializer.Serialize(ref obj[i], mode, stream);
                }
            }
        }
    }
}
