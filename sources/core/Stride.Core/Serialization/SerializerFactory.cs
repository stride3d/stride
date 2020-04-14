// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Storage;

namespace Xenko.Core.Serialization
{
    /// <summary>
    /// Used as a fallback when <see cref="SerializerSelector.GetSerializer"/> didn't find anything.
    /// </summary>
    public abstract class SerializerFactory
    {
        public abstract DataSerializer GetSerializer(SerializerSelector selector, ref ObjectId typeId);
        public abstract DataSerializer GetSerializer(SerializerSelector selector, Type type);
    }
}
