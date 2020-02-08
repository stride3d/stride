using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ServiceWire;

namespace Xenko.Core.BuildEngine.Common
{
    public class XenkoJSONSerializer : ISerializer
    {
        private static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
        };
        public T Deserialize<T>(byte[] bytes)
        {
            if (null == bytes || bytes.Length == 0) return default(T);
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes), settings);
        }

        public object Deserialize(byte[] bytes, string typeConfigName)
        {
            if (null == typeConfigName || null == bytes || bytes.Length == 0) return null;
            var type = typeConfigName.ToType();
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), type, settings);
        }

        public byte[] Serialize<T>(T obj)
        {
            if (null == obj) return null;
            var json = JsonConvert.SerializeObject(obj, settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public byte[] Serialize(object obj, string typeConfigName)
        {
            if (null == obj) return null;
            var type = typeConfigName.ToType();
            var json = JsonConvert.SerializeObject(obj, type, settings);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
