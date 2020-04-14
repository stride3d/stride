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

using Stride.Core.Yaml.Events;

namespace Stride.Core.Yaml.Schemas
{
    /// <summary>
    /// Implements the YAML failsafe schema.
    /// <see cref="http://www.yaml.org/spec/1.2/spec.html#id2802346" />
    /// </summary>
    /// <remarks>The failsafe schema is guaranteed to work with any YAML document.
    /// It is therefore the recommended schema for generic YAML tools.
    /// A YAML processor should therefore support this schema, at least as an option.</remarks>
    public class FailsafeSchema : SchemaBase
    {
        /// <summary>
        /// The map short tag: !!map.
        /// </summary>
        public const string MapShortTag = "!!map";


        /// <summary>
        /// The map long tag: tag:yaml.org,2002:map
        /// </summary>
        public const string MapLongTag = "tag:yaml.org,2002:map";

        /// <summary>
        /// The seq short tag: !!seq
        /// </summary>
        public const string SeqShortTag = "!!seq";

        /// <summary>
        /// The seq long tag: tag:yaml.org,2002:seq
        /// </summary>
        public const string SeqLongTag = "tag:yaml.org,2002:seq";

        /// <summary>
        /// Initializes a new instance of the <see cref="FailsafeSchema"/> class.
        /// </summary>
        public FailsafeSchema()
        {
            RegisterTag(MapShortTag, MapLongTag);
            RegisterTag(SeqShortTag, SeqLongTag);
            RegisterTag(StrShortTag, StrLongTag);
            AllowFailsafeString = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this schema should always fallback to a
        /// failsafe string in case of not matching any scalar rules. Default is true for <see cref="FailsafeSchema"/>
        /// </summary>
        /// <value><c>true</c> if [allow failsafe string]; otherwise, <c>false</c>.</value>
        protected bool AllowFailsafeString { get; set; }

        protected override string GetDefaultTag(MappingStart nodeEvent)
        {
            return MapShortTag;
        }

        protected override string GetDefaultTag(SequenceStart nodeEvent)
        {
            return SeqShortTag;
        }

        public override bool TryParse(Scalar scalar, bool parseValue, out string defaultTag, out object value)
        {
            if (base.TryParse(scalar, parseValue, out defaultTag, out value))
            {
                return true;
            }

            if (AllowFailsafeString)
            {
                value = parseValue ? scalar.Value : null;
                defaultTag = StrShortTag;
                return true;
            }

            return false;
        }
    }
}