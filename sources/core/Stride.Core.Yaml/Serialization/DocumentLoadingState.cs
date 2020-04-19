// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Manages the state of a <see cref="YamlDocument" /> while it is loading.
    /// </summary>
    internal class DocumentLoadingState
    {
        private readonly IDictionary<string, YamlNode> anchors = new Dictionary<string, YamlNode>();
        private readonly IList<YamlNode> nodesWithUnresolvedAliases = new List<YamlNode>();

        /// <summary>
        /// Adds the specified node to the anchor list.
        /// </summary>
        /// <param name="node">The node.</param>
        public void AddAnchor(YamlNode node)
        {
            if (node.Anchor == null)
            {
                throw new ArgumentException("The specified node does not have an anchor");
            }

            if (anchors.ContainsKey(node.Anchor))
            {
                throw new DuplicateAnchorException(node.Start, node.End, string.Format(CultureInfo.InvariantCulture, "The anchor '{0}' already exists", node.Anchor));
            }

            anchors.Add(node.Anchor, node);
        }

        /// <summary>
        /// Gets the node with the specified anchor.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <param name="throwException">if set to <c>true</c>, the method should throw an exception if there is no node with that anchor.</param>
        /// <param name="start">The start position.</param>
        /// <param name="end">The end position.</param>
        /// <returns></returns>
        public YamlNode GetNode(string anchor, bool throwException, Mark start, Mark end)
        {
            YamlNode target;
            if (anchors.TryGetValue(anchor, out target))
            {
                return target;
            }
            else if (throwException)
            {
                throw new AnchorNotFoundException(anchor, start, end, string.Format(CultureInfo.InvariantCulture, "The anchor '{0}' does not exists", anchor));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Adds the specified node to the collection of nodes with unresolved aliases.
        /// </summary>
        /// <param name="node">
        /// The <see cref="YamlNode"/> that has unresolved aliases.
        /// </param>
        public void AddNodeWithUnresolvedAliases(YamlNode node)
        {
            nodesWithUnresolvedAliases.Add(node);
        }

        /// <summary>
        /// Resolves the aliases that could not be resolved while loading the document.
        /// </summary>
        public void ResolveAliases()
        {
            foreach (var node in nodesWithUnresolvedAliases)
            {
                node.ResolveAliases(this);

#if DEBUG
                foreach (var child in node.AllNodes)
                {
                    if (child is YamlAliasNode)
                    {
                        throw new InvalidOperationException("Error in alias resolution.");
                    }
                }
#endif
            }
        }
    }
}