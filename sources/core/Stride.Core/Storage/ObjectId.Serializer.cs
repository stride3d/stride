// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Serialization;

namespace Stride.Core.Storage
{
    /// <summary>
    /// A hash to uniquely identify data.
    /// </summary>
    [DataSerializer(typeof(ObjectId.Serializer))]
    public unsafe partial struct ObjectId
    {
        internal class Serializer : DataSerializer<ObjectId>
        {
            public override void Serialize(ref ObjectId obj, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Serialize)
                {
                    var hasId = obj != Empty;
                    stream.Write(hasId);
                    if (hasId)
                    {
                        fixed (uint* hash = &obj.hash1)
                            stream.Serialize((IntPtr)hash, HashSize);
                    }
                }
                else if (mode == ArchiveMode.Deserialize)
                {
                    var hasId = stream.ReadBoolean();
                    if (hasId)
                    {
                        var id = new byte[HashSize];
                        stream.Serialize(id, 0, HashSize);
                        obj = new ObjectId(id);
                    }
                    else
                    {
                        obj = ObjectId.Empty;
                    }
                }
            }
        }
    }
}
