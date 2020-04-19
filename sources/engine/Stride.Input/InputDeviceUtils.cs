// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Stride.Core.Serialization;
using Stride.Core.Storage;

namespace Stride.Input
{
    /// <summary>
    /// Utilities for input devices
    /// </summary>
    public static class InputDeviceUtils
    {
        /// <summary>
        /// Generates a Guid unique to this name
        /// </summary>
        /// <param name="name">the name to turn into a Guid</param>
        /// <returns>A unique Guid for the given name</returns>
        public static Guid DeviceNameToGuid(string name)
        {
            MemoryStream stream = new MemoryStream();
            DigestStream writer = new DigestStream(stream);
            {
                BinarySerializationWriter serializer = new HashSerializationWriter(writer);
                serializer.Write(typeof(IInputDevice).GetHashCode());
                serializer.Write(name);
            }

            return writer.CurrentHash.ToGuid();
        }
    }
}