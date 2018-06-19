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
using NUnit.Framework;
using Xenko.Core.Yaml.Events;
using Xenko.Core.Yaml.Schemas;

namespace Xenko.Core.Yaml.Tests
{
    public class SchemaTests
    {
        [Test]
        public void TestFailsafeSchema()
        {
            var schema = new FailsafeSchema();
            TestFailsafeSchemaCommon(schema);
        }

        [Test]
        public void TestJsonSchema()
        {
            var schema = new JsonSchema();
            TestJsonSchemaCommon(schema);

            // Json should not accept plain literal
            Assert.AreEqual(null, schema.GetDefaultTag(new Scalar(null, null, "boom", ScalarStyle.Plain, true, false)));
        }

        [Test]
        public void TestCoreSchema()
        {
            var schema = new CoreSchema();

            TestCoreSchemaCommon(schema);
        }

        [Test]
        public void TestExtendedSchema()
        {
            var schema = new ExtendedSchema();

            TestCoreSchemaCommon(schema);

            TryParse(schema, "2002-12-14", ExtendedSchema.TimestampShortTag, new DateTime(2002, 12, 14));
            TryParse(schema, "2002-12-14 21:59:43.234", ExtendedSchema.TimestampShortTag, new DateTime(2002, 12, 14, 21, 59, 43, 234));
        }

        public void TestFailsafeSchemaCommon(IYamlSchema schema)
        {
            Assert.AreEqual(SchemaBase.StrShortTag, schema.GetDefaultTag(new Scalar("true")));
            Assert.AreEqual(SchemaBase.StrShortTag, schema.GetDefaultTag(new Scalar("custom", "boom")));
            Assert.AreEqual(FailsafeSchema.MapShortTag, schema.GetDefaultTag(new MappingStart()));
            Assert.AreEqual(FailsafeSchema.SeqShortTag, schema.GetDefaultTag(new SequenceStart()));

            Assert.AreEqual(FailsafeSchema.MapLongTag, schema.ExpandTag("!!map"));
            Assert.AreEqual(FailsafeSchema.SeqLongTag, schema.ExpandTag("!!seq"));
            Assert.AreEqual(SchemaBase.StrLongTag, schema.ExpandTag("!!str"));

            Assert.AreEqual("!!map", schema.ShortenTag(FailsafeSchema.MapLongTag));
            Assert.AreEqual("!!seq", schema.ShortenTag(FailsafeSchema.SeqLongTag));
            Assert.AreEqual("!!str", schema.ShortenTag(SchemaBase.StrLongTag));

            TryParse(schema, "true", SchemaBase.StrShortTag, "true");
        }

        public void TestJsonSchemaCommon(IYamlSchema schema)
        {
            Assert.AreEqual(SchemaBase.StrShortTag, schema.GetDefaultTag(new Scalar(null, null, "true", ScalarStyle.DoubleQuoted, false, false)));
            Assert.AreEqual(JsonSchema.BoolShortTag, schema.GetDefaultTag(new Scalar("true")));
            Assert.AreEqual(JsonSchema.NullShortTag, schema.GetDefaultTag(new Scalar("null")));
            Assert.AreEqual(JsonSchema.IntShortTag, schema.GetDefaultTag(new Scalar("5")));
            Assert.AreEqual(JsonSchema.FloatShortTag, schema.GetDefaultTag(new Scalar("5.5")));

            Assert.AreEqual(JsonSchema.NullLongTag, schema.ExpandTag("!!null"));
            Assert.AreEqual(JsonSchema.BoolLongTag, schema.ExpandTag("!!bool"));
            Assert.AreEqual(JsonSchema.IntLongTag, schema.ExpandTag("!!int"));
            Assert.AreEqual(JsonSchema.FloatLongTag, schema.ExpandTag("!!float"));

            Assert.AreEqual("!!null", schema.ShortenTag(JsonSchema.NullLongTag));
            Assert.AreEqual("!!bool", schema.ShortenTag(JsonSchema.BoolLongTag));
            Assert.AreEqual("!!int", schema.ShortenTag(JsonSchema.IntLongTag));
            Assert.AreEqual("!!float", schema.ShortenTag(JsonSchema.FloatLongTag));

            TryParse(schema, "null", JsonSchema.NullShortTag, null);
            TryParse(schema, "true", JsonSchema.BoolShortTag, true);
            TryParse(schema, "false", JsonSchema.BoolShortTag, false);
            TryParse(schema, "5", JsonSchema.IntShortTag, 5);
            TryParse(schema, "5.5", JsonSchema.FloatShortTag, 5.5);
            TryParse(schema, ".inf", JsonSchema.FloatShortTag, double.PositiveInfinity);
        }

        public void TestCoreSchemaCommon(IYamlSchema schema)
        {
            TestJsonSchemaCommon(schema);

            // Core schema is accepting plain string
            Assert.AreEqual(SchemaBase.StrShortTag, schema.GetDefaultTag(new Scalar("boom")));
            Assert.AreEqual(JsonSchema.BoolShortTag, schema.GetDefaultTag(new Scalar("True")));
            Assert.AreEqual(JsonSchema.BoolShortTag, schema.GetDefaultTag(new Scalar("TRUE")));
            Assert.AreEqual(JsonSchema.IntShortTag, schema.GetDefaultTag(new Scalar("0x10")));

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
            Assert.AreEqual(expectedShortTag, tag);
            Assert.AreEqual(expectedValue, value);
        }
    }
}