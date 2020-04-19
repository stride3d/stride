// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;

using Stride.Core.Reflection;

namespace Stride.Core.Serialization.Serializers
{
    public class PropertyKeySerializer<T> : DataSerializer<T> where T : PropertyKey
    {
        public override void Serialize(ref T obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Name);
                stream.Write(obj.OwnerType.AssemblyQualifiedName);
            }
            else
            {
                var parameterName = stream.ReadString();
                var ownerTypeName = stream.ReadString();
                var ownerType = AssemblyRegistry.GetType(ownerTypeName);

                obj = (T)ownerType.GetTypeInfo().GetDeclaredField(parameterName).GetValue(null);
            }
        }
    }
}
