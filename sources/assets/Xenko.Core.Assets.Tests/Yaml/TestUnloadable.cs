// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml;

namespace Xenko.Core.Assets.Tests.Yaml
{
    [TestFixture]
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

        [Test]
        public void TestInvalidTypeToObject()
        {
            var obj = Deserialize<object>(YamlInvalidType);

            // Check it's an unloadable object
            Assert.IsInstanceOf<IUnloadable>(obj);
            Assert.IsNotInstanceOf<UnloadableContainer>(obj);

            // Make sure it is stable when serialized
            Assert.AreEqual(YamlInvalidType, Serialize(obj));
        }

        [Test]
        public void TestInvalidTypeToGivenType()
        {
            var obj = Deserialize<UnloadableContainer>(YamlInvalidType);

            // Check it's an unloadable object
            Assert.IsInstanceOf<IUnloadable>(obj);
            // Check that properties of the expected type deserialized properly
            Assert.AreEqual("value0", obj.ObjectMember);

            // Make sure it is stable when serialized
            Assert.AreEqual(YamlInvalidType, Serialize(obj));
        }

        [Test]
        public void TestUnknownMember()
        {
            var obj = Deserialize<UnloadableContainer>(YamlUnknownMember);

            // Check it's not an unloadable object
            Assert.IsNotInstanceOf<IUnloadable>(obj);
            // Check that properties of the type deserialized properly
            Assert.AreEqual("value1", obj.ObjectMember);

            // It shouldn't be stable since a member is gone
            Assert.AreNotEqual(YamlInvalidType, Serialize(obj));
        }

        [Test]
        public void TestInvalidMemberType()
        {
            var obj = Deserialize<UnloadableContainer>(YamlInvalidMemberType);

            // Check it's not an unloadable object
            Assert.IsNotInstanceOf<IUnloadable>(obj);
            // But it's member should be
            Assert.IsInstanceOf<IUnloadable>(obj.ObjectMember);

            // It shouldn't be stable since a member is gone
            Assert.AreNotEqual(YamlInvalidType, Serialize(obj));
        }

        [Test]
        public void TestInvalidCollectionItemType()
        {
            var obj = Deserialize<UnloadableContainer>(YamlInvalidCollectionItemType);

            // Check it's not an unloadable object
            Assert.IsNotInstanceOf<IUnloadable>(obj);
            // Check collection members
            Assert.IsNotInstanceOf<IUnloadable>(obj.ObjectList[0]);
            Assert.AreEqual("value0", obj.ObjectList[0]);
            Assert.IsInstanceOf<IUnloadable>(obj.ObjectList[1]);

            // It shouldn't be stable since a member is gone
            Assert.AreNotEqual(YamlInvalidType, Serialize(obj));
        }
    }
}
