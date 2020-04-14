// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Serialization;

namespace Stride.Shaders
{
    /// <summary>
    /// Description of a StreamOutput declaration entry.
    /// </summary>
    [DataContract]
    public struct ShaderStreamOutputDeclarationEntry
    {
        /// <summary>
        /// The stream index.
        /// </summary>
        public int Stream;

        /// <summary>
        /// The semantic name.
        /// </summary>
        public string SemanticName;

        /// <summary>
        /// The semantic index.
        /// </summary>
        public int SemanticIndex;

        /// <summary>
        /// The start component
        /// </summary>
        public byte StartComponent;

        /// <summary>
        /// The component count
        /// </summary>
        public byte ComponentCount;

        /// <summary>
        /// The output slot
        /// </summary>
        public byte OutputSlot;
    }
}
