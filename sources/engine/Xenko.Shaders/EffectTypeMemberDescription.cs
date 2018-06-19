// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using Xenko.Core;

namespace Xenko.Shaders
{
    /// <summary>
    /// Describes a shader parameter member.
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{Name}: {Type}")]
    public struct EffectTypeMemberDescription
    {
        /// <summary>
        /// The name of this member.
        /// </summary>
        public string Name;

        /// <summary>
        /// Offset in bytes into the parent structure (0 if not a structure member).
        /// </summary>
        public int Offset;

        /// <summary>
        /// The type of this member.
        /// </summary>
        public EffectTypeDescription Type;
    }
}
