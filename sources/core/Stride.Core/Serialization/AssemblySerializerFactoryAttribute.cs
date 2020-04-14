// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Core.Serialization
{
    /// <summary>
    /// Used internally by assembly processor when generating serializer factories.
    /// </summary>
    public class AssemblySerializerFactoryAttribute
    {
        /// <summary>
        /// The type of the serializer factory.
        /// </summary>
        public Type Type;
    }
}
