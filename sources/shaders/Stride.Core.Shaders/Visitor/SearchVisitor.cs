// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Visitor
{
    /// <summary>
    /// A visitor that takes a filter function to apply to each node.
    /// </summary>
    public class SearchVisitor : ShaderRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchVisitor"/> class.
        /// </summary>
        /// <param name="filterFunction">The filter function.</param>
        /// <param name="buildScopeDeclaration">if set to <c>true</c> [build scope declaration].</param>
        /// <param name="useNodeStack">if set to <c>true</c> [use node stack].</param>
        protected SearchVisitor(Func<Node, Node> filterFunction, bool buildScopeDeclaration = false, bool useNodeStack = false)
            : base(buildScopeDeclaration, useNodeStack)
        {
            FilterFunction = filterFunction;
        }

        /// <summary>
        /// Gets or sets the filter function.
        /// </summary>
        /// <value>
        /// The filter function.
        /// </value>
        protected Func<Node, Node> FilterFunction { get; set; }

        /// <summary>
        /// Visits the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The filtered node</returns>
        public override Node DefaultVisit(Node node)
        {
            node = FilterFunction(node);
            if (node != null)
                node = base.DefaultVisit(node);

            return node;
        }

        /// <summary>
        /// Searches from the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="filter">The filter function to apply to each node.</param>
        /// <param name="buildScopeDeclaration">if set to <c>true</c> [build scope declaration].</param>
        /// <param name="useNodeStack">if set to <c>true</c> [use node stack].</param>
        public static void Run(Node node, Func<Node, Node> filter, bool buildScopeDeclaration = false, bool useNodeStack = false)
        {
            var visitor = new SearchVisitor(filter, buildScopeDeclaration, useNodeStack);
            visitor.VisitDynamic(node);
        }
    }
}
