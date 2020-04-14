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
using System.Globalization;
using System.Text.RegularExpressions;

namespace Stride.Core.Yaml.Schemas
{
    /// <summary>
    /// Implements a JSON schema. <see cref="http://www.yaml.org/spec/1.2/spec.html#id2803231" />
    /// </summary>
    /// <remarks>
    /// The JSON schema is the lowest common denominator of most modern computer languages, and allows parsing JSON files. 
    /// A YAML processor should therefore support this schema, at least as an option. It is also strongly recommended that other schemas should be based on it. .
    /// </remarks>>
    public class JsonSchema : FailsafeSchema
    {
        /// <summary>
        /// The null short tag: !!null
        /// </summary>
        public const string NullShortTag = "!!null";

        /// <summary>
        /// The null long tag: tag:yaml.org,2002:null
        /// </summary>
        public const string NullLongTag = "tag:yaml.org,2002:null";

        /// <summary>
        /// The bool short tag: !!bool
        /// </summary>
        public const string BoolShortTag = "!!bool";

        /// <summary>
        /// The bool long tag: tag:yaml.org,2002:bool
        /// </summary>
        public const string BoolLongTag = "tag:yaml.org,2002:bool";

        /// <summary>
        /// The int short tag: !!int
        /// </summary>
        public const string IntShortTag = "!!int";

        /// <summary>
        /// The int long tag: tag:yaml.org,2002:int
        /// </summary>
        public const string IntLongTag = "tag:yaml.org,2002:int";

        /// <summary>
        /// The float short tag: !!float
        /// </summary>
        public const string FloatShortTag = "!!float";

        /// <summary>
        /// The float long tag: tag:yaml.org,2002:float
        /// </summary>
        public const string FloatLongTag = "tag:yaml.org,2002:float";

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSchema"/> class.
        /// </summary>
        public JsonSchema()
        {
            RegisterTag(NullShortTag, NullLongTag);
            RegisterTag(BoolShortTag, BoolLongTag);
            RegisterTag(IntShortTag, IntLongTag);
            RegisterTag(FloatShortTag, FloatLongTag);
        }

        protected override void PrepareScalarRules()
        {
            // 10.2.1.1. Null
            AddScalarRule<object>("!!null", @"null", m => null, null);

            // 10.2.1.2. Boolean
            AddScalarRule<bool>("!!bool", @"true", m => true, null);
            AddScalarRule<bool>("!!bool", @"false", m => false, null);

            // 10.2.1.3. Integer
            AddScalarRule(new Type[] {typeof(ulong), typeof(long), typeof(int)}, "!!int", @"((0|-?[1-9][0-9_]*))", DecodeInteger, null);

            // 10.2.1.4. Floating Point
            AddScalarRule<double>("!!float", @"-?(0|[1-9][0-9]*)(\.[0-9]*)?([eE][-+]?[0-9]+)?", m => Convert.ToDouble(m.Value.Replace("_", ""), CultureInfo.InvariantCulture), null);
            AddScalarRule<double>("!!float", @"\.inf", m => double.PositiveInfinity, null);
            AddScalarRule<double>("!!float", @"-\.inf", m => double.NegativeInfinity, null);
            AddScalarRule<double>("!!float", @"\.nan", m => double.NaN, null);

            // Json doesn't allow failsafe string, so we are disabling it here.
            AllowFailsafeString = false;

            // We are not calling the base as we want to completely override scalar rules
            // and in order to have a more concise set of regex
        }

        protected object DecodeInteger(Match m)
        {
            var valueStr = m.Value.Replace("_", "");
            int value;
            // Try plain native int first 
            if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }
            // Else long
            long result;
            if (long.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            // Else ulong
            return ulong.Parse(valueStr, CultureInfo.InvariantCulture);
        }

        protected override void RegisterDefaultTagMappings()
        {
            base.RegisterDefaultTagMappings();

            // All bool type
            RegisterDefaultTagMapping<bool>(BoolShortTag, true);

            // All int types
            RegisterDefaultTagMapping<sbyte>(IntShortTag);
            RegisterDefaultTagMapping<byte>(IntShortTag);
            RegisterDefaultTagMapping<short>(IntShortTag);
            RegisterDefaultTagMapping<ushort>(IntShortTag);
            RegisterDefaultTagMapping<int>(IntShortTag, true);
            RegisterDefaultTagMapping<uint>(IntShortTag);
            RegisterDefaultTagMapping<long>(IntShortTag);
            RegisterDefaultTagMapping<ulong>(IntShortTag);

            // All double/float types
            RegisterDefaultTagMapping<float>(FloatShortTag, true);
            RegisterDefaultTagMapping<double>(FloatShortTag);

            // All string types
            RegisterDefaultTagMapping<char>(StrShortTag);
            RegisterDefaultTagMapping<string>(StrShortTag, true);
        }
    }
}