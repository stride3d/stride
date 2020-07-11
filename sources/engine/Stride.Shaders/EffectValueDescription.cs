// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Shaders
{
    /// <summary>
    /// Describes a shader parameter for a valuetype (usually stored in constant buffers).
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{Type.Class}{Type.RowCount}x{Type.ColumnCount} {KeyInfo.KeyName} -> {RawName}")]
    public struct EffectValueDescription
    {
        /// <summary>
        /// The type of this value.
        /// </summary>
        public EffectTypeDescription Type;

        /// <summary>
        /// The common description of this parameter.
        /// </summary>
        public EffectParameterKeyInfo KeyInfo;

        /// <summary>
        /// Name of this parameter in the original shader
        /// </summary>
        public string RawName;
        
        /// <summary>
        /// Offset in bytes into the constant buffer.
        /// </summary>
        public int Offset;

        /// <summary>
        /// Size in bytes in a constant buffer.
        /// </summary>
        public int Size;

        /// <summary>
        /// The default value.
        /// </summary>
        public object DefaultValue;

        /// <summary>
        /// Logical group, used to group related descriptors and variables together.
        /// </summary>
        public string LogicalGroup;
    }
}
