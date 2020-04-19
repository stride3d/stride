// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Shaders
{
    /// <summary>
    /// Structure containing SPIR-V bytecode, as well as mappings from input attribute locations to semantics.
    /// </summary>
    [DataContract]
    public struct ShaderInputBytecode
    {
        public Dictionary<int, string> InputAttributeNames;

        public Dictionary<string, int> ResourceBindings;

        public byte[] Data;
    }
}
