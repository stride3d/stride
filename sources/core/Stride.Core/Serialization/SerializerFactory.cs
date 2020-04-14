// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Storage;

namespace Stride.Core.Serialization
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
