// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if !XENKO_PLATFORM_UWP && !XENKO_RUNTIME_CORECLR
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Xenko.Engine.Network
{
    // TODO: Switch to internal serialization engine
    public class SocketSerializer
    {
        private BinaryFormatter binaryFormatter = new BinaryFormatter();

        public Stream Stream { get; set; }

        public void Serialize(object obj)
        {
            if (Stream == null)
                return;
            lock (this)
            {
                binaryFormatter.Serialize(Stream, obj);
            }
        }
        public object Deserialize()
        {
            return binaryFormatter.Deserialize(Stream);
        }
    }
}
#endif
