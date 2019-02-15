// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Graphics;

namespace Xenko.Rendering
{
    /// <summary>
    /// Represents a <see cref="RenderObject"/> drawn with a specific <see cref="RenderEffect"/>, with attached properties.
    /// </summary>
    public struct EffectObjectNode
    {
        /// <summary>
        /// The effect used.
        /// </summary>
        public RenderEffect RenderEffect;

        /// <summary>
        /// The object node reference.
        /// </summary>
        public ObjectNodeReference ObjectNode;

        /// <summary>
        /// The "PerObject" descriptor set.
        /// </summary>
        public DescriptorSet ObjectDescriptorSet;

        /// <summary>
        /// The "PerObject" constant buffer offset in our global cbuffer.
        /// </summary>
        public int ObjectConstantBufferOffset;

        public EffectObjectNode(RenderEffect renderEffect, ObjectNodeReference objectNode) : this()
        {
            RenderEffect = renderEffect;
            ObjectNode = objectNode;
        }
    }
}
