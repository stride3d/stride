// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Serialization;
using Stride.Core.Storage;
using Stride.Rendering;

namespace Stride.Shaders
{
    class ParameterKeyHashSerializer : DataSerializer<ParameterKey>
    {
        public unsafe override void Serialize(ref ParameterKey obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode != ArchiveMode.Serialize)
                throw new InvalidOperationException();

            // Just use parameter key hash code
            // Hopefully there won't be any clash...
            fixed (ulong* objId = &obj.HashCode)
            {
                stream.Serialize(ref *objId);
            }
        }
    }
}
