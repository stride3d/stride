using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServiceWire;

namespace Stride.Core.Serialization.Serializers
{
    [DataSerializerGlobal(typeof(ServiceWireSerializer))]
    public class ServiceWireSerializer : DataSerializer<ServiceSyncInfo>
    {
        public override void PreSerialize(ref ServiceSyncInfo obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                if (obj == null)
                    obj = new ServiceSyncInfo();
            }
        }

        public override void Serialize(ref ServiceSyncInfo info, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(info.CompressionThreshold);
                stream.Write(info.ServiceKeyIndex);
                stream.Write(info.UseCompression);
                stream.Write(info.MethodInfos.Length);
                foreach (var method in info.MethodInfos)
                {
                    stream.Write(method.MethodIdent);
                    stream.Write(method.MethodName);
                    stream.Write(method.MethodReturnType);
                    stream.Write(method.ParameterTypes.Length);
                    foreach (var paramType in method.ParameterTypes)
                    {
                        stream.Write(paramType);
                    }
                }
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                info.CompressionThreshold = stream.ReadInt32();
                info.ServiceKeyIndex = stream.ReadInt32();
                info.UseCompression = stream.ReadBoolean();
                var length = stream.ReadInt32();
                info.MethodInfos = new MethodSyncInfo[length];
                for (int i = 0; i < length; i++)
                {
                    info.MethodInfos[i] = new MethodSyncInfo();
                    info.MethodInfos[i].MethodIdent = stream.ReadInt32();
                    info.MethodInfos[i].MethodName = stream.ReadString();
                    info.MethodInfos[i].MethodReturnType = stream.ReadString();
                    var paramLength = stream.ReadInt32();
                    info.MethodInfos[i].ParameterTypes = new string[paramLength];
                    for (int j = 0; j < paramLength; j++)
                    {
                        info.MethodInfos[i].ParameterTypes[j] = stream.ReadString();
                    }
                }
            }
        }
    }
}
