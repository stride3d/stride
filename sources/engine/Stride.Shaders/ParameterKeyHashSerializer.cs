// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Serialization;
using Xenko.Core.Storage;
using Xenko.Rendering;

namespace Xenko.Shaders
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
