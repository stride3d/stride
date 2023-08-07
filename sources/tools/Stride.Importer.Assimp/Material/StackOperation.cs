// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Importer.Assimp.Material
{
    /// <summary>
    /// Class representing an operation in the new Assimp's material stack.
    /// </summary>
    public class StackOperation : StackElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StackOperation"/> class.
        /// </summary>
        /// <param name="operation">The operation of the node.</param>
        /// <param name="alpha">The alpha of the node.</param>
        /// <param name="blend">The blending coefficient of the node.</param>
        /// <param name="flags">The flags.</param>
        public StackOperation(Operation operation, float alpha = 1.0f, float blend = 1.0f, int flags = 0)
            : base(alpha, blend, flags, StackType.Operation)
        {
            Operation = operation;
        }
        /// <summary>
        /// Gets the operation of the node.
        /// </summary>
        /// <value>
        /// The operation of the node.
        /// </value>
        public Operation Operation { get; private set; }
    }
}
