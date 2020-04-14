// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using Stride.Core;
using Stride.Core.Storage;

namespace Stride.Shaders
{
    /// <summary>
    /// Description of a constant buffer.
    /// </summary>
    [DataContract]
    [DebuggerDisplay("cbuffer {Name} : {Size} bytes")]
    public class EffectConstantBufferDescription
    {
        /// <summary>
        /// The name of this constant buffer.
        /// </summary>
        public string Name;

        /// <summary>
        /// The size in bytes.
        /// </summary>
        public int Size;

        /// <summary>
        /// The type of constant buffer.
        /// </summary>
        public ConstantBufferType Type;

        /// <summary>
        /// The members of this constant buffer.
        /// </summary>
        public EffectValueDescription[] Members;

        [DataMemberIgnore]
        public ObjectId Hash;

        /// <summary>
        /// Clone the current instance of the constant buffer description.
        /// </summary>
        /// <returns>A clone copy of the description</returns>
        public EffectConstantBufferDescription Clone()
        {
            return (EffectConstantBufferDescription)MemberwiseClone();
        }
    }
}
