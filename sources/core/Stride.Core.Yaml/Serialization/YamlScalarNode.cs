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
using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Represents a scalar node in the YAML document.
    /// </summary>
    [DebuggerDisplay("{Value}")]
    public class YamlScalarNode : YamlNode
    {
        /// <summary>
        /// Gets or sets the value of the node.
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the style of the node.
        /// </summary>
        /// <value>The style.</value>
        public ScalarStyle Style { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlScalarNode"/> class.
        /// </summary>
        /// <param name="events">The events.</param>
        /// <param name="state">The state.</param>
        internal YamlScalarNode(EventReader events, DocumentLoadingState state)
        {
            Scalar scalar = events.Expect<Scalar>();
            Load(scalar, state);
            Value = scalar.Value;
            Style = scalar.Style;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlScalarNode"/> class.
        /// </summary>
        public YamlScalarNode()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YamlScalarNode"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public YamlScalarNode(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Resolves the aliases that could not be resolved when the node was created.
        /// </summary>
        /// <param name="state">The state of the document.</param>
        internal override void ResolveAliases(DocumentLoadingState state)
        {
            throw new NotSupportedException("Resolving an alias on a scalar node does not make sense");
        }

        /// <summary>
        /// Saves the current node to the specified emitter.
        /// </summary>
        /// <param name="emitter">The emitter where the node is to be saved.</param>
        /// <param name="state">The state.</param>
        internal override void Emit(IEmitter emitter, EmitterState state)
        {
            emitter.Emit(new Scalar(Anchor, Tag, Value, Style, string.IsNullOrEmpty(Tag), false));
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
            var obj = other as YamlScalarNode;
            return obj != null && Equals(obj) && SafeEquals(Value, obj.Value);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return CombineHashCodes(
                base.GetHashCode(),
                GetHashCode(Value)
                );
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="YamlScalarNode"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator YamlScalarNode(string value)
        {
            return new YamlScalarNode(value);
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="YamlScalarNode"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator string(YamlScalarNode value)
        {
            return value.Value;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Gets all nodes from the document, starting on the current node.
        /// </summary>
        public override IEnumerable<YamlNode> AllNodes { get { yield return this; } }
    }
}