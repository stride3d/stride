// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Importer.Assimp.Material
{
    /// <summary>
    /// Class representing an element in the new Assimp's material stack.
    /// </summary>
    public abstract class StackElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StackElement"/> class.
        /// </summary>
        /// <param name="alpha">The alpha of the node.</param>
        /// <param name="blend">The blending coefficient of the node.</param>
        /// <param name="flags">The flags of the node.</param>
        /// <param name="type">The type of the node.</param>
        public StackElement(float alpha, float blend, int flags, StackElementType type)
        {
            Alpha = alpha;
            Blend = blend;
            Type = type;
        }
        /// <summary>
        /// Gets the alpha of the node.
        /// </summary>
        /// <value>
        /// The alpha of the node.
        /// </value>
        public float Alpha { get; private set; }
        /// <summary>
        /// Gets the blending coefficient of the node.
        /// </summary>
        /// <value>
        /// The blending coefficient of the node.
        /// </value>
        public float Blend { get; private set; }
        /// <summary>
        /// Gets the flags of the node.
        /// </summary>
        /// <value>
        /// The flags of the node.
        /// </value>
        public int Flags { get; private set; }
        /// <summary>
        /// Gets the type of the node.
        /// </summary>
        /// <value>
        /// The type of the node.
        /// </value>
        public StackElementType Type { get; private set; }
    }
}
