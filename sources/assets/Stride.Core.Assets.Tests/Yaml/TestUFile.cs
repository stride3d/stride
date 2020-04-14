// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tests.Yaml
{
    public class TestUFile
    {
        [DataContract("TestUFileClass")]
        public class TestUFileClass
        {
            public UFile File { get; set; }
        }

        private const string YamlWithFileTag = @"!TestUFileClass
File: !file test.txt
";

        private const string YamlWithoutFileTag = @"!TestUFileClass
File: test.txt
";

        private static string Serialize(object instance)
        {
            using (var stream = new MemoryStream())
            {
                AssetYamlSerializer.Default.Serialize(stream, instance);
                stream.Flush();
                stream.Position = 0;
                return new StreamReader(stream).ReadToEnd();
            }
        }

        private static T Deserialize<T>(string data)
        {
            using (var stream = new MemoryStream())
            {
                var streamWriter = new StreamWriter(stream);
                streamWriter.Write(data);
                streamWriter.Flush();
                stream.Position = 0;

                return (T)AssetYamlSerializer.Default.Deserialize(stream, typeof(T));
            }
        }

        [Fact]
        public void TestWithFileTagRoundtrip()
        {
            // Check deserialization works
            var obj = Deserialize<TestUFileClass>(YamlWithFileTag);
            Assert.Equal("test.txt", obj.File);

            // Check object is serialized back properly
            var yaml = Serialize(obj);
            Assert.Equal(YamlWithFileTag, yaml);
        }

        [Fact]
        public void TestWithoutFileTagRoundtrip()
        {
            // Check deserialization works
            var obj = Deserialize<TestUFileClass>(YamlWithoutFileTag);
            Assert.Equal("test.txt", obj.File);

            // Check object is serialized back properly (it should have the file tag now)
            var yaml = Serialize(obj);
            Assert.Equal(YamlWithFileTag, yaml);
        }
    }
}
