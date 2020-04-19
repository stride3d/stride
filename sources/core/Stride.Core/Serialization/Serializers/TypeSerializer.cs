// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Reflection;

namespace Stride.Core.Serialization.Serializers
{
    [DataSerializerGlobal(typeof(TypeSerializer))]
    public class TypeSerializer : DataSerializer<Type>
    {
        public override void Serialize(ref Type type, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(type.AssemblyQualifiedName);
            }
            else
            {
                var typeName = stream.ReadString();
                type = AssemblyRegistry.GetType(typeName);
            }
        }
    }
}
