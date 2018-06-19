// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Core.AssemblyProcessor
{
    [Flags]
    public enum ComplexTypeSerializerFlags
    {
        SerializePublicFields = 1,
        SerializePublicProperties = 2,

        /// <summary>
        /// If the member has DataMemberIgnore and DataMemberUpdatable, it will be included
        /// </summary>
        Updatable = 4,
    }
}
