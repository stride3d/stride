// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Core.Serialization
{
    /// <summary>
    /// A null serializer that can be used to add dummy serialization attributes.
    /// </summary>
    public class NullSerializer<T> : DataSerializer<T>
    {
        public override void Serialize(ref T obj, ArchiveMode mode, SerializationStream stream)
        {
            throw new NotImplementedException();
        }
    }
}
