// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using Xenko.Core;

namespace Xenko.Shaders
{
    /// <summary>
    /// Describes a shader parameter for a resource type.
    /// </summary>
    [DataContract]
    [DebuggerDisplay("[{Stage}] {Class} {KeyInfo.Key} -> {RawName}")]
    public struct EffectResourceBindingDescription
    {
        /// <summary>
        /// The common description of this parameter.
        /// </summary>
        public EffectParameterKeyInfo KeyInfo;

        /// <summary>
        /// The <see cref="EffectParameterClass"/> of this parameter.
        /// </summary>
        public EffectParameterClass Class;

        /// <summary>
        /// The <see cref="EffectParameterType"/> of this parameter.
        /// </summary>
        public EffectParameterType Type;

        /// <summary>
        /// The element type (for buffers or textures).
        /// </summary>
        public EffectTypeDescription ElementType;

        /// <summary>
        /// Name of this parameter in the original shader
        /// </summary>
        public string RawName;

        /// <summary>
        /// Resource group this variable belongs to. This should later be directly grouped in EffectReflection.ResourceGroups.
        /// </summary>
        public string ResourceGroup;

        /// <summary>
        /// The stage this parameter is used
        /// </summary>
        public ShaderStage Stage;

        /// <summary>
        /// The starting slot this parameter is bound.
        /// </summary>
        public int SlotStart;

        /// <summary>
        /// The number of slots bound to this parameter starting at <see cref="SlotStart"/>.
        /// </summary>
        public int SlotCount;

        /// <summary>
        /// Logical group, used to group related descriptors and variables together.
        /// </summary>
        public string LogicalGroup;
    }
}
