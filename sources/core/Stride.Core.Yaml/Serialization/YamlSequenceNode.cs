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
using System.Diagnostics;
using System.Text;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Represents a sequence node in the YAML document.
    /// </summary>
    [DebuggerDisplay("Count = {children.Count}")]
    public class YamlSequenceNode : YamlNode, IEnumerable<YamlNode>
    {
        private readonly IList<YamlNode> children = new List<YamlNode>();

        /// <summary>
        /// Gets the collection of child nodes.
        /// </summary>
        /// <value>The children.</value>
        public IList<YamlNode> Children { get { return children; } }

        /// <summary>
        /// Gets or sets the style of the node.
        /// </summary>
        /// <value>The style.</value>
        public DataStyle Style { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="YamlSequenceNode"/> class.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="state">The state.</param>
        internal YamlSequenceNode(EventReader events, DocumentLoadingState state)
        {
            SequenceStart sequence = events.Expect<SequenceStart>();
            Load(sequence, state);
            Style = sequence.Style;

            bool hasUnresolvedAliases = false;
            while (!events.Accept<SequenceEnd>())
            {
                YamlNode child = ParseNode(events, state);
                children.Add(child);
                hasUnresolvedAliases |= child is YamlAliasNode;
            }

            if (hasUnresolvedAliases)
            {
                state.AddNodeWithUnresolvedAliases(this);
            }
#if DEBUG
            else
            {
                foreach (var child in children)
                {
                    if (child is YamlAliasNode)
                    {
                        throw new InvalidOperationException("Error in alias resolution.");
                    }
                }
            }
#endif

            events.Expect<SequenceEnd>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlSequenceNode"/> class.
        /// </summary>
        public YamlSequenceNode()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlSequenceNode"/> class.
        /// </summary>
        public YamlSequenceNode(params YamlNode[] children)
            : this((IEnumerable<YamlNode>) children)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlSequenceNode"/> class.
        /// </summary>
        public YamlSequenceNode(IEnumerable<YamlNode> children)
        {
            foreach (var child in children)
            {
                this.children.Add(child);
            }
        }

        /// <summary>
        /// Adds the specified child to the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="child">The child.</param>
        public void Add(YamlNode child)
        {
            children.Add(child);
        }

        /// <summary>
        /// Adds a scalar node to the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="child">The child.</param>
        public void Add(string child)
        {
            children.Add(new YamlScalarNode(child));
        }

        /// <summary>
        /// Resolves the aliases that could not be resolved when the node was created.
        /// </summary>
        /// <param name="state">The state of the document.</param>
        internal override void ResolveAliases(DocumentLoadingState state)
        {
            for (int i = 0; i < children.Count; ++i)
            {
                if (children[i] is YamlAliasNode)
                {
                    children[i] = state.GetNode(children[i].Anchor, true, children[i].Start, children[i].End);
                }
            }
        }

        /// <summary>
        /// Saves the current node to the specified emitter.
        /// </summary>
        /// <param name="emitter">The emitter where the node is to be saved.</param>
        /// <param name="state">The state.</param>
        internal override void Emit(IEmitter emitter, EmitterState state)
        {
            emitter.Emit(new SequenceStart(Anchor, Tag, string.IsNullOrEmpty(Tag), Style));
            foreach (var node in children)
            {
                node.Save(emitter, state);
            }
            emitter.Emit(new SequenceEnd());
        }

        /// <summary>
        /// Accepts the specified visitor by calling the appropriate Visit method on it.
        /// </summary>
        /// <param name="visitor">
        /// A <see cref="IYamlVisitor"/>.
        /// </param>
        public override void Accept(IYamlVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <summary />
        public override bool Equals(object other)
        {
            var obj = other as YamlSequenceNode;
            if (obj == null || !Equals(obj) || children.Count != obj.children.Count)
            {
                return false;
            }

            for (int i = 0; i < children.Count; ++i)
            {
                if (!SafeEquals(children[i], obj.children[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode();

            foreach (var item in children)
            {
                hashCode = CombineHashCodes(hashCode, GetHashCode(item));
            }
            return hashCode;
        }

        /// <summary>
        /// Gets all nodes from the document, starting on the current node.
        /// </summary>
        public override IEnumerable<YamlNode> AllNodes
        {
            get
            {
                yield return this;
                foreach (var child in children)
                {
                    foreach (var node in child.AllNodes)
                    {
                        yield return node;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var text = new StringBuilder("[ ");

            foreach (var child in children)
            {
                if (text.Length > 2)
                {
                    text.Append(", ");
                }
                text.Append(child);
            }

            text.Append(" ]");

            return text.ToString();
        }

        #region IEnumerable<YamlNode> Members

        /// <summary />
        public IEnumerator<YamlNode> GetEnumerator()
        {
            return Children.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}