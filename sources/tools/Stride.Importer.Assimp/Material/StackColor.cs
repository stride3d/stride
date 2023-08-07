// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Importer.Assimp.Material
{
    /// <summary>
    /// Class representing a color in the new Assimp's material stack.
    /// </summary>
    public class StackColor : StackElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StackColor"/> class.
        /// </summary>
        /// <param name="color">The color of the node.</param>
        /// <param name="alpha">The alpha of the node.</param>
        /// <param name="blend">The blending coefficient of the node.</param>
        /// <param name="flags">The flags of the node.</param>
        public StackColor(Color3 color, float alpha = 1.0f, float blend = 1.0f, int flags = 0)
            : base(alpha, blend, flags, StackType.Color)
        {
            Color = color;
        }
        /// <summary>
        /// Gets the color of the node.
        /// </summary>
        /// <value>
        /// The color of the node.
        /// </value>
        public Color3 Color { get; private set; }
    }
}
