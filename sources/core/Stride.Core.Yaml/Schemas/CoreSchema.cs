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
    /// Implements the Core schema. <see cref="http://www.yaml.org/spec/1.2/spec.html#id2804356" />
    /// </summary>
    /// <remarks>
    /// The Core schema is an extension of the JSON schema, allowing for more human-readable presentation of the same types. 
    /// This is the recommended default schema that YAML processor should use unless instructed otherwise. 
    /// It is also strongly recommended that other schemas should be based on it. 
    /// </remarks>
    public class CoreSchema : JsonSchema
    {
        protected override void PrepareScalarRules()
        {
            // 10.2.1.1. Null
            AddScalarRule<object>("!!null", @"null|Null|NULL|\~", m => null, null);

            AddScalarRule<bool>("!!bool", @"true|True|TRUE", m => true, null);
            AddScalarRule<bool>("!!bool", @"false|False|FALSE", m => false, null);

            AddScalarRule(new Type[] {typeof(ulong), typeof(long), typeof(int)}, "!!int", @"([-+]?(0|[1-9][0-9_]*))", DecodeInteger, null);

            // Make float before 0x/0o to improve parsing as float are more common than 0x and 0o
            AddScalarRule<double>("!!float", @"[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?", m => Convert.ToDouble(m.Value.Replace("_", ""), CultureInfo.InvariantCulture), null);

            AddScalarRule<int>("!!int", @"0x([0-9a-fA-F_]+)", m => Convert.ToInt32(m.Groups[1].Value.Replace("_", ""), 16), null);
            AddScalarRule<int>("!!int", @"0o([0-7_]+)", m => Convert.ToInt32(m.Groups[1].Value.Replace("_", ""), 8), null);

            AddScalarRule<double>("!!float", @"\+?(\.inf|\.Inf|\.INF)", m => double.PositiveInfinity, null);
            AddScalarRule<double>("!!float", @"-(\.inf|\.Inf|\.INF)", m => double.NegativeInfinity, null);
            AddScalarRule<double>("!!float", @"\.nan|\.NaN|\.NAN", m => double.NaN, null);

            AllowFailsafeString = true;

            // We are not calling the base as we want to completely override scalar rules
            // and in order to have a more concise set of regex
        }
    }
}