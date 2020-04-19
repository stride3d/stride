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
using Xunit;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Schemas;

namespace Stride.Core.Yaml.Tests
{
    public class SchemaTests
    {
        [Fact]
        public void TestFailsafeSchema()
        {
            var schema = new FailsafeSchema();
            TestFailsafeSchemaCommon(schema);
        }

        [Fact]
        public void TestJsonSchema()
        {
            var schema = new JsonSchema();
            TestJsonSchemaCommon(schema);

            // Json should not accept plain literal
            Assert.Null(schema.GetDefaultTag(new Scalar(null, null, "boom", ScalarStyle.Plain, true, false)));
        }

        [Fact]
        public void TestCoreSchema()
        {
            var schema = new CoreSchema();

            TestCoreSchemaCommon(schema);
        }

        [Fact]
        public void TestExtendedSchema()
        {
            var schema = new ExtendedSchema();

            TestCoreSchemaCommon(schema);

            TryParse(schema, "2002-12-14", ExtendedSchema.TimestampShortTag, new DateTime(2002, 12, 14));
            TryParse(schema, "2002-12-14 21:59:43.234", ExtendedSchema.TimestampShortTag, new DateTime(2002, 12, 14, 21, 59, 43, 234));
        }

        private void TestFailsafeSchemaCommon(IYamlSchema schema)
        {
            Assert.Equal(SchemaBase.StrShortTag, schema.GetDefaultTag(new Scalar("true")));
            Assert.Equal(SchemaBase.StrShortTag, schema.GetDefaultTag(new Scalar("custom", "boom")));
            Assert.Equal(FailsafeSchema.MapShortTag, schema.GetDefaultTag(new MappingStart()));
            Assert.Equal(FailsafeSchema.SeqShortTag, schema.GetDefaultTag(new SequenceStart()));

            Assert.Equal(FailsafeSchema.MapLongTag, schema.ExpandTag("!!map"));
            Assert.Equal(FailsafeSchema.SeqLongTag, schema.ExpandTag("!!seq"));
            Assert.Equal(SchemaBase.StrLongTag, schema.ExpandTag("!!str"));

            Assert.Equal("!!map", schema.ShortenTag(FailsafeSchema.MapLongTag));
            Assert.Equal("!!seq", schema.ShortenTag(FailsafeSchema.SeqLongTag));
            Assert.Equal("!!str", schema.ShortenTag(SchemaBase.StrLongTag));

            TryParse(schema, "true", SchemaBase.StrShortTag, "true");
        }

        private void TestJsonSchemaCommon(IYamlSchema schema)
        {
            Assert.Equal(SchemaBase.StrShortTag, schema.GetDefaultTag(new Scalar(null, null, "true", ScalarStyle.DoubleQuoted, false, false)));
            Assert.Equal(JsonSchema.BoolShortTag, schema.GetDefaultTag(new Scalar("true")));
            Assert.Equal(JsonSchema.NullShortTag, schema.GetDefaultTag(new Scalar("null")));
            Assert.Equal(JsonSchema.IntShortTag, schema.GetDefaultTag(new Scalar("5")));
            Assert.Equal(JsonSchema.FloatShortTag, schema.GetDefaultTag(new Scalar("5.5")));

            Assert.Equal(JsonSchema.NullLongTag, schema.ExpandTag("!!null"));
            Assert.Equal(JsonSchema.BoolLongTag, schema.ExpandTag("!!bool"));
            Assert.Equal(JsonSchema.IntLongTag, schema.ExpandTag("!!int"));
            Assert.Equal(JsonSchema.FloatLongTag, schema.ExpandTag("!!float"));

            Assert.Equal("!!null", schema.ShortenTag(JsonSchema.NullLongTag));
            Assert.Equal("!!bool", schema.ShortenTag(JsonSchema.BoolLongTag));
            Assert.Equal("!!int", schema.ShortenTag(JsonSchema.IntLongTag));
            Assert.Equal("!!float", schema.ShortenTag(JsonSchema.FloatLongTag));

            TryParse(schema, "null", JsonSchema.NullShortTag, null);
            TryParse(schema, "true", JsonSchema.BoolShortTag, true);
            TryParse(schema, "false", JsonSchema.BoolShortTag, false);
            TryParse(schema, "5", JsonSchema.IntShortTag, 5);
            TryParse(schema, "5.5", JsonSchema.FloatShortTag, 5.5);
            TryParse(schema, ".inf", JsonSchema.FloatShortTag, double.PositiveInfinity);
        }

        private void TestCoreSchemaCommon(IYamlSchema schema)
        {
            TestJsonSchemaCommon(schema);

            // Core schema is accepting plain string
            Assert.Equal(SchemaBase.StrShortTag, schema.GetDefaultTag(new Scalar("boom")));
            Assert.Equal(JsonSchema.BoolShortTag, schema.GetDefaultTag(new Scalar("True")));
            Assert.Equal(JsonSchema.BoolShortTag, schema.GetDefaultTag(new Scalar("TRUE")));
            Assert.Equal(JsonSchema.IntShortTag, schema.GetDefaultTag(new Scalar("0x10")));

            TryParse(schema, "TRUE", JsonSchema.BoolShortTag, true);
            TryParse(schema, "FALSE", JsonSchema.BoolShortTag, false);
            TryParse(schema, "0x10", JsonSchema.IntShortTag, 16);
            TryParse(schema, "16", JsonSchema.IntShortTag, 16);
        }

        private void TryParse(IYamlSchema schema, string scalar, string expectedShortTag, object expectedValue)
        {
            string tag;
            object value;
            Assert.True(schema.TryParse(new Scalar(scalar), true, out tag, out value));
            Assert.Equal(expectedShortTag, tag);
            Assert.Equal(expectedValue, value);
        }
    }
}
