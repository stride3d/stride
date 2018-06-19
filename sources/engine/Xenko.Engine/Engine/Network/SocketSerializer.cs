// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if !XENKO_PLATFORM_UWP && !XENKO_RUNTIME_CORECLR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Xenko.Engine.Network
{
    // TODO: Switch to internal serialization engine
    public class SocketSerializer
    {
        public Stream Stream { get; set; }
        BinaryFormatter binaryFormatter = new BinaryFormatter();
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
