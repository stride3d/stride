using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ServiceWire;

namespace Stride.VisualStudio.Commands
{

    internal class ServiceWireBinaryFormatterSerializer : ISerializer
    {
        private readonly IFormatter _formatter = new BinaryFormatter();

        public byte[] Serialize<T>(T obj)
        {
            if (null == obj) return null;
            using (var ms = new MemoryStream())
            {
                _formatter.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public byte[] Serialize(object obj, string typeConfigName)
        {
            if (null == obj) return null;
            using (var ms = new MemoryStream())
            {
                var type = typeConfigName.ToType();
                var objT = Convert.ChangeType(obj, type);
                _formatter.Serialize(ms, objT);
                return ms.ToArray();
            }
        }

        public T Deserialize<T>(byte[] bytes)
        {
            if (null == bytes || bytes.Length == 0) return default(T);
            using (var ms = new MemoryStream(bytes))
            {
                return (T)_formatter.Deserialize(ms);
            }
        }

        public object Deserialize(byte[] bytes, string typeConfigName)
        {
            if (null == typeConfigName) throw new ArgumentNullException(nameof(typeConfigName));
            var type = typeConfigName.ToType();
            if (null == typeConfigName || null == bytes || bytes.Length == 0) return type.GetDefault();
            using (var ms = new MemoryStream(bytes))
            {
                var obj = _formatter.Deserialize(ms);
                return Convert.ChangeType(obj, type);
            }
        }
    }
}
