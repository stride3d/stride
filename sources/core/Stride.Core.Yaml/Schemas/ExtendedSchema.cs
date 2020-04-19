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

namespace Stride.Core.Yaml.Schemas
{
    /// <summary>
    /// Extension to the core schema and accept different flavor of scalars
    /// <ul>
    /// <li>bool(true):  y|Y|yes|Yes|YES|true|True|TRUE|on|On|ON</li>
    /// <li>bool(false): n|N|no|No|NO|false|False|FALSE|off|Off|OFF</li>
    /// <li>timestamp</li>
    /// </ul>
    /// </summary>
    public class ExtendedSchema : CoreSchema
    {
        /// <summary>
        /// The timestamp short tag: !!timestamp
        /// </summary>
        public const string TimestampShortTag = "!!timestamp";

        /// <summary>
        /// The timestamp long tag: tag:yaml.org,2002:timestamp
        /// </summary>
        public const string TimestampLongTag = "tag:yaml.org,2002:timestamp";

        /// <summary>
        /// The merge short tag: !!merge
        /// </summary>
        public const string MergeShortTag = "!!merge";

        /// <summary>
        /// The merge long tag: tag:yaml.org,2002:merge
        /// </summary>
        public const string MergeLongTag = "tag:yaml.org,2002:merge";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedSchema"/> class.
        /// </summary>
        public ExtendedSchema()
        {
            RegisterTag(TimestampShortTag, TimestampLongTag);
            RegisterTag(MergeShortTag, MergeLongTag);
        }

        protected override void PrepareScalarRules()
        {
            AddScalarRule<object>("!!null", @"null|Null|NULL|\~|", m => null, null);
            AddScalarRule(new Type[] {typeof(ulong), typeof(long), typeof(int)}, "!!int", @"([-+]?(0|[1-9][0-9_]*))", DecodeInteger, null);
            AddScalarRule<int>("!!int", @"([-+]?)0b([01_]+)", m =>
            {
                var v = Convert.ToInt32(m.Groups[2].Value.Replace("_", ""), 2);
                return m.Groups[1].Value == "-" ? -v : v;
            }, null);
            AddScalarRule<int>("!!int", @"([-+]?)0o?([0-7_]+)", m =>
            {
                var v = Convert.ToInt32(m.Groups[2].Value.Replace("_", ""), 8);
                return m.Groups[1].Value == "-" ? -v : v;
            }, null);
            AddScalarRule<int>("!!int", @"([-+]?)0x([0-9a-fA-F_]+)", m =>
            {
                var v = Convert.ToInt32(m.Groups[2].Value.Replace("_", ""), 16);
                return m.Groups[1].Value == "-" ? -v : v;
            }, null);
            // Todo: http://yaml.org/type/float.html is wrong  => [0-9.] should be [0-9_]
            AddScalarRule<double>("!!float", @"[-+]?(0|[1-9][0-9_]*)\.[0-9_]*([eE][-+]?[0-9]+)?",
                m => Convert.ToDouble(m.Value.Replace("_", ""), CultureInfo.InvariantCulture), null);
            AddScalarRule<double>("!!float", @"[-+]?\._*[0-9][0-9_]*([eE][-+]?[0-9]+)?",
                m => Convert.ToDouble(m.Value.Replace("_", "")), null);
            AddScalarRule<double>("!!float", @"[-+]?(0|[1-9][0-9_]*)([eE][-+]?[0-9]+)",
                m => Convert.ToDouble(m.Value.Replace("_", ""), CultureInfo.InvariantCulture), null);
            AddScalarRule<double>("!!float", @"\+?(\.inf|\.Inf|\.INF)", m => double.PositiveInfinity, null);
            AddScalarRule<double>("!!float", @"-(\.inf|\.Inf|\.INF)", m => double.NegativeInfinity, null);
            AddScalarRule<double>("!!float", @"\.nan|\.NaN|\.NAN", m => double.NaN, null);
            AddScalarRule<bool>("!!bool", @"y|Y|yes|Yes|YES|true|True|TRUE|on|On|ON", m => true, null);
            AddScalarRule<bool>("!!bool", @"n|N|no|No|NO|false|False|FALSE|off|Off|OFF", m => false, null);
            AddScalarRule<string>("!!merge", @"<<", m => "<<", null);
            AddScalarRule<DateTime>("!!timestamp", // Todo: spec is wrong (([ \t]*)Z|[-+][0-9][0-9]?(:[0-9][0-9])?)? should be (([ \t]*)(Z|[-+][0-9][0-9]?(:[0-9][0-9])?))? to accept "2001-12-14 21:59:43.10 -5"
                @"([0-9]{4})-([0-9]{2})-([0-9]{2})" +
                @"(" +
                @"([Tt]|[\t ]+)" +
                @"([0-9]{1,2}):([0-9]{1,2}):([0-9]{1,2})(\.([0-9]*))?" +
                @"(" +
                @"([ \t]*)" +
                @"(Z|([-+])([0-9]{1,2})(:([0-9][0-9]))?)" +
                @")?" +
                @")?",
                match => DateTime.Parse(match.Value, CultureInfo.InvariantCulture),
                datetime =>
                {
                    var z = datetime.ToString("%K", CultureInfo.InvariantCulture);
                    if (z != "Z" && z != "")
                        z = " " + z;
                    if (datetime.Millisecond == 0)
                    {
                        if (datetime.Hour == 0 && datetime.Minute == 0 && datetime.Second == 0)
                        {
                            return datetime.ToString("yyyy-MM-dd" + z, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            return datetime.ToString("yyyy-MM-dd HH:mm:ss" + z, CultureInfo.InvariantCulture);
                        }
                    }
                    else
                    {
                        return datetime.ToString("yyyy-MM-dd HH:mm:ss.fff" + z, CultureInfo.InvariantCulture);
                    }
                });

            AllowFailsafeString = true;

            // We are not calling the base as we want to completely override scalar rules
            // and in order to have a more concise set of regex
        }

        protected override void RegisterDefaultTagMappings()
        {
            base.RegisterDefaultTagMappings();
            RegisterDefaultTagMapping<DateTime>(TimestampShortTag, true);
        }
    }
}