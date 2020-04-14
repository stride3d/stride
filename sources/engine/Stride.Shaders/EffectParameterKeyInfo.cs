// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Rendering;

namespace Stride.Shaders
{
    /// <summary>
    /// The header of a shader parameter.
    /// </summary>
    [DataContract]
    public struct EffectParameterKeyInfo
    {
        /// <summary>
        /// The key of the parameter.
        /// </summary>
        [DataMemberIgnore]
        public ParameterKey Key;

        /// <summary>
        /// The key name.
        /// </summary>
        public string KeyName;
    }
}
