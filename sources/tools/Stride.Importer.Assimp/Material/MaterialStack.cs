// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.Importer.Assimp.Material
{
    /// <summary>
    /// Class representing the new Assimp's material stack in c#.
    /// </summary>
    public class MaterialStack
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialStack"/> class.
        /// </summary>
        public MaterialStack()
        {
            stack = new Stack<StackElement>();
        }
        /// <summary>
        /// The internal stack.
        /// </summary>
        private Stack<StackElement> stack;
        /// <summary>
        /// Gets the size of the stack.
        /// </summary>
        /// <value>
        /// The size of the stack.
        /// </value>
        private int Count { get { return stack.Count; } }
        /// <summary>
        /// Gets a value indicating whether the stack is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the stack is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty { get { return stack.Count == 0; } }
        /// <summary>
        /// Pushes the specified element.
        /// </summary>
        /// <param name="element">The element.</param>
        public void Push(StackElement element)
        {
            stack.Push(element);
        }
        /// <summary>
        /// Pops an element.
        /// </summary>
        /// <returns>The element.</returns>
        public StackElement Pop()
        {
            return stack.Pop();
        }
        /// <summary>
        /// Gets the top element of the stack.
        /// </summary>
        /// <returns>The top element of the stack.</returns>
        public StackElement Peek()
        {
            return stack.Peek();
        }
        /// <summary>
        /// Clears the stack.
        /// </summary>
        public void Clear()
        {
            stack.Clear();
        }
    }
}
