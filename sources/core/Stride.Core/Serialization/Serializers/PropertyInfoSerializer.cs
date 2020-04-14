// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;

using Stride.Core.Reflection;

namespace Stride.Core.Serialization.Serializers
{
    public class PropertyInfoSerializer : DataSerializer<PropertyInfo>
    {
        public override void Serialize(ref PropertyInfo propertyInfo, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(propertyInfo.DeclaringType.AssemblyQualifiedName);
                stream.Write(propertyInfo.Name);
            }
            else
            {
                var declaringTypeName = stream.ReadString();
                var propertyName = stream.ReadString();

                var ownerType = AssemblyRegistry.GetType(declaringTypeName);
                if (ownerType == null)
                    throw new InvalidOperationException("Could not find the appropriate type.");

                propertyInfo = ownerType.GetTypeInfo().GetDeclaredProperty(propertyName);
            }
        }
    }
}
