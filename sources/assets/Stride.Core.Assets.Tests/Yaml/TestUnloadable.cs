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

namespace Stride.Core.Assets.Tests.Yaml
{
    public class TestUnloadable
    {
        [DataContract("UnloadableContainer")]
        public class UnloadableContainer
        {
            public List<object> ObjectList { get; } = new List<object>();
            public object ObjectMember { get; set; }
        }

        private const string YamlInvalidType = @"!UnloadableContainerInvalidType
ObjectMember: value0
";

        private const string YamlUnknownMember = @"!UnloadableContainer
UnknownMember: value0
ObjectMember: value1
";

        private const string YamlInvalidMemberType = @"!UnloadableContainer
ObjectMember: !UnknownType value0
";

        private const string YamlInvalidCollectionItemType = @"!UnloadableContainer
ObjectList:
    01000000010000000100000001000000: value0
    02000000020000000200000002000000: !UnknownType value1
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
        public void TestInvalidTypeToObject()
        {
            var obj = Deserialize<object>(YamlInvalidType);

            // Check it's an unloadable object
            Assert.IsAssignableFrom<IUnloadable>(obj);
            Assert.False(obj is UnloadableContainer);

            // Make sure it is stable when serialized
            Assert.Equal(YamlInvalidType, Serialize(obj));
        }

        [Fact]
        public void TestInvalidTypeToGivenType()
        {
            var obj = Deserialize<UnloadableContainer>(YamlInvalidType);

            // Check it's an unloadable object
            Assert.IsAssignableFrom<IUnloadable>(obj);
            // Check that properties of the expected type deserialized properly
            Assert.Equal("value0", obj.ObjectMember);

            // Make sure it is stable when serialized
            Assert.Equal(YamlInvalidType, Serialize(obj));
        }

        [Fact]
        public void TestUnknownMember()
        {
            var obj = Deserialize<UnloadableContainer>(YamlUnknownMember);

            // Check it's not an unloadable object
            Assert.False(obj is IUnloadable);
            // Check that properties of the type deserialized properly
            Assert.Equal("value1", obj.ObjectMember);

            // It shouldn't be stable since a member is gone
            Assert.NotEqual(YamlInvalidType, Serialize(obj));
        }

        [Fact]
        public void TestInvalidMemberType()
        {
            var obj = Deserialize<UnloadableContainer>(YamlInvalidMemberType);

            // Check it's not an unloadable object
            Assert.False(obj is IUnloadable);
            // But it's member should be
            Assert.IsAssignableFrom<IUnloadable>(obj.ObjectMember);

            // It shouldn't be stable since a member is gone
            Assert.NotEqual(YamlInvalidType, Serialize(obj));
        }

        [Fact]
        public void TestInvalidCollectionItemType()
        {
            var obj = Deserialize<UnloadableContainer>(YamlInvalidCollectionItemType);

            // Check it's not an unloadable object
            Assert.False(obj is IUnloadable);
            // Check collection members
            Assert.False(obj.ObjectList[0] is IUnloadable);
            Assert.Equal("value0", obj.ObjectList[0]);
            Assert.True(obj.ObjectList[1] is IUnloadable);

            // It shouldn't be stable since a member is gone
            Assert.NotEqual(YamlInvalidType, Serialize(obj));
        }
    }
}
