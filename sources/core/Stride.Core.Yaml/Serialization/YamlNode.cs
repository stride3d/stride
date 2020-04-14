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
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Represents a single node in the YAML document.
    /// </summary>
    public abstract class YamlNode
    {
        /// <summary>
        /// Gets or sets the anchor of the node.
        /// </summary>
        /// <value>The anchor.</value>
        public string Anchor { get; set; }

        /// <summary>
        /// Gets or sets the tag of the node.
        /// </summary>
        /// <value>The tag.</value>
        public string Tag { get; set; }

        /// <summary>
        /// Gets the position in the input stream where the event that originated the node starts.
        /// </summary>
        public Mark Start { get; private set; }

        /// <summary>
        /// Gets the position in the input stream where the event that originated the node ends.
        /// </summary>
        public Mark End { get; private set; }

        /// <summary>
        /// Loads the specified event.
        /// </summary>
        /// <param name="yamlEvent">The event.</param>
        /// <param name="state">The state of the document.</param>
        internal void Load(NodeEvent yamlEvent, DocumentLoadingState state)
        {
            Tag = yamlEvent.Tag;
            if (yamlEvent.Anchor != null)
            {
                Anchor = yamlEvent.Anchor;
                state.AddAnchor(this);
            }
            Start = yamlEvent.Start;
            End = yamlEvent.End;
        }

        /// <summary>
        /// Parses the node represented by the next event in <paramref name="events" />.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="state">The state.</param>
        /// <returns>Returns the node that has been parsed.</returns>
        static internal YamlNode ParseNode(EventReader events, DocumentLoadingState state)
        {
            if (events.Accept<Scalar>())
            {
                return new YamlScalarNode(events, state);
            }

            if (events.Accept<SequenceStart>())
            {
                return new YamlSequenceNode(events, state);
            }

            if (events.Accept<MappingStart>())
            {
                return new YamlMappingNode(events, state);
            }

            if (events.Accept<AnchorAlias>())
            {
                AnchorAlias alias = events.Expect<AnchorAlias>();
                return state.GetNode(alias.Value, false, alias.Start, alias.End) ?? new YamlAliasNode(alias.Value);
            }

            throw new ArgumentException("The current event is of an unsupported type.", "events");
        }

        /// <summary>
        /// Resolves the aliases that could not be resolved when the node was created.
        /// </summary>
        /// <param name="state">The state of the document.</param>
        internal abstract void ResolveAliases(DocumentLoadingState state);

        /// <summary>
        /// Saves the current node to the specified emitter.
        /// </summary>
        /// <param name="emitter">The emitter where the node is to be saved.</param>
        /// <param name="state">The state.</param>
        internal void Save(IEmitter emitter, EmitterState state)
        {
            if (!string.IsNullOrEmpty(Anchor) && !state.EmittedAnchors.Add(Anchor))
            {
                emitter.Emit(new AnchorAlias(Anchor));
            }
            else
            {
                Emit(emitter, state);
            }
        }

        /// <summary>
        /// Saves the current node to the specified emitter.
        /// </summary>
        /// <param name="emitter">The emitter where the node is to be saved.</param>
        /// <param name="state">The state.</param>
        internal abstract void Emit(IEmitter emitter, EmitterState state);

        /// <summary>
        /// Accepts the specified visitor by calling the appropriate Visit method on it.
        /// </summary>
        /// <param name="visitor">
        /// A <see cref="IYamlVisitor"/>.
        /// </param>
        public abstract void Accept(IYamlVisitor visitor);

        /// <summary>
        /// Provides a basic implementation of Object.Equals 
        /// </summary>
        protected bool Equals(YamlNode other)
        {
            // Do not use the anchor in the equality comparison because that would prevent anchored nodes from being found in dictionaries.
            return SafeEquals(Tag, other.Tag);
        }

        /// <summary>
        /// Gets a value indicating whether two objects are equal.
        /// </summary>
        protected static bool SafeEquals(object first, object second)
        {
            if (first != null)
            {
                return first.Equals(second);
            }
            else if (second != null)
            {
                return second.Equals(first);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            // Do not use the anchor in the hash code because that would prevent anchored nodes from being found in dictionaries.
            return GetHashCode(Tag);
        }

        /// <summary>
        /// Gets the hash code of the specified object, or zero if the object is null. 
        /// </summary>
        protected static int GetHashCode(object value)
        {
            return value == null ? 0 : value.GetHashCode();
        }

        /// <summary>
        /// Combines two hash codes into one. 
        /// </summary>
        protected static int CombineHashCodes(int h1, int h2)
        {
            return unchecked(((h1 << 5) + h1) ^ h2);
        }

        /// <summary>
        /// Gets all nodes from the document, starting on the current node.
        /// </summary>
        public abstract IEnumerable<YamlNode> AllNodes { get; }
    }
}