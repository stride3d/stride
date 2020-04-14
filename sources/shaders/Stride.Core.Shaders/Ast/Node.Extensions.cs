// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Extensions for <see cref="Node"/>.
    /// </summary>
    public static class NodeExtensions
    {
        /// <summary>
        /// Get descendants for the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>An enumeration of descendants</returns>
        private static IEnumerable<Node> DescendantsImpl(this Node node)
        {
            if (node != null)
            {
                yield return node;

                foreach (var children in node.Childrens())
                {
                    if (children != null)
                        foreach (var descendant in children.Descendants())
                        {
                            yield return descendant;
                        }
                }
            }
        }

        /// <summary>
        /// Get descendants for the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>An enumeration of descendants</returns>
        public static IEnumerable<Node> Descendants(this Node node)
        {
            if (node != null)
            {
                foreach (var children in node.Childrens())
                {
                    if (children != null)
                        foreach (var descendant in children.DescendantsImpl())
                        {
                            yield return descendant;
                        }
                }
            }
        }
    }
}
