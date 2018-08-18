// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Serialization;

namespace Xenko.Animations
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
                int count = obj.Length;
                var rawData = stream.ReadBytes(Utilities.SizeOf<AnimationKeyValuePair<T>>() * count);
                fixed (void* rawDataPtr = rawData)
                {
                    Utilities.Read((IntPtr)rawDataPtr, obj, 0, count);
                }
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
