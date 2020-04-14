// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Assets.Tests.Yaml
{
    public class TestCollectionIdsSerialization
    {
        public class ContainerCollection
        {
            public ContainerCollection() { }
            public ContainerCollection(string name)
            {
                Name = name;
            }
            public string Name { get; set; }
            public List<string> Strings { get; set; } = new List<string>();
            public List<ContainerCollection> Objects { get; set; } = new List<ContainerCollection>();
        }

        public class ContainerDictionary
        {
            public ContainerDictionary() { }
            public ContainerDictionary(string name)
            {
                Name = name;
            }
            public string Name { get; set; }
            public Dictionary<Guid, string> Strings { get; set; } = new Dictionary<Guid, string>();
            public Dictionary<string, ContainerCollection> Objects { get; set; } = new Dictionary<string, ContainerCollection>();
        }

        public class ContainerNonIdentifiableCollection
        {
            public ContainerNonIdentifiableCollection() { }
            public ContainerNonIdentifiableCollection(string name)
            {
                Name = name;
            }
            public string Name { get; set; }
            public List<ContainerCollection> Objects { get; set; } = new List<ContainerCollection>();

            [NonIdentifiableCollectionItems]
            public List<ContainerCollection> NonIdentifiableObjects { get; set; } = new List<ContainerCollection>();
        }

        public class ContainerNonIdentifiableDictionary
        {
            public ContainerNonIdentifiableDictionary() { }
            public ContainerNonIdentifiableDictionary(string name)
            {
                Name = name;
            }
            public string Name { get; set; }
            public Dictionary<string, ContainerCollection> Objects { get; set; } = new Dictionary<string, ContainerCollection>();

            [NonIdentifiableCollectionItems]
            public Dictionary<string, ContainerCollection> NonIdentifiableObjects { get; set; } = new Dictionary<string, ContainerCollection>();
        }


        private const string YamlCollection = @"!Stride.Core.Assets.Tests.Yaml.TestCollectionIdsSerialization+ContainerCollection,Stride.Core.Assets.Tests
Name: Root
Strings:
    02000000020000000200000002000000: aaa
    01000000010000000100000001000000: bbb
Objects:
    03000000030000000300000003000000:
        Name: obj1
        Strings: {}
        Objects: {}
    04000000040000000400000004000000:
        Name: obj2
        Strings: {}
        Objects: {}
";

        private const string YamlCollectionNotIdentifiable = @"!Stride.Core.Assets.Tests.Yaml.TestCollectionIdsSerialization+ContainerNonIdentifiableCollection,Stride.Core.Assets.Tests
Name: Root
Objects:
    02000000020000000200000002000000:
        Name: aaa
        Strings:
            05000000050000000500000005000000: bbb
            06000000060000000600000006000000: ccc
        Objects: {}
    01000000010000000100000001000000:
        Name: ddd
        Strings:
            07000000070000000700000007000000: eee
            08000000080000000800000008000000: fff
        Objects: {}
NonIdentifiableObjects:
    -   Name: ggg
        Strings:
            09000000090000000900000009000000: hhh
            0a0000000a0000000a0000000a000000: iii
        Objects: {}
    -   Name: jjj
        Strings:
            0b0000000b0000000b0000000b000000: kkk
            0c0000000c0000000c0000000c000000: lll
        Objects: {}
";

        private const string YamlDictionary = @"!Stride.Core.Assets.Tests.Yaml.TestCollectionIdsSerialization+ContainerDictionary,Stride.Core.Assets.Tests
Name: Root
Strings:
    02000000020000000200000002000000~000000c8-00c8-0000-c800-0000c8000000: aaa
    01000000010000000100000001000000~00000064-0064-0000-6400-000064000000: bbb
Objects:
    03000000030000000300000003000000~key3:
        Name: obj1
        Strings: {}
        Objects: {}
    04000000040000000400000004000000~key4:
        Name: obj2
        Strings: {}
        Objects: {}
";

        private const string YamlDictionaryNonIdentifiable = @"!Stride.Core.Assets.Tests.Yaml.TestCollectionIdsSerialization+ContainerNonIdentifiableDictionary,Stride.Core.Assets.Tests
Name: Root
Objects:
    02000000020000000200000002000000~AAA:
        Name: aaa
        Strings:
            05000000050000000500000005000000: bbb
            06000000060000000600000006000000: ccc
        Objects: {}
    01000000010000000100000001000000~BBB:
        Name: ddd
        Strings:
            07000000070000000700000007000000: eee
            08000000080000000800000008000000: fff
        Objects: {}
NonIdentifiableObjects:
    CCC:
        Name: ggg
        Strings:
            09000000090000000900000009000000: hhh
            0a0000000a0000000a0000000a000000: iii
        Objects: {}
    DDD:
        Name: jjj
        Strings:
            0b0000000b0000000b0000000b000000: kkk
            0c0000000c0000000c0000000c000000: lll
        Objects: {}
";

        private const string YamlCollectionWithDeleted = @"!Stride.Core.Assets.Tests.Yaml.TestCollectionIdsSerialization+ContainerCollection,Stride.Core.Assets.Tests
Name: Root
Strings:
    08000000080000000800000008000000: aaa
    05000000050000000500000005000000: bbb
    01000000010000000100000001000000: ~(Deleted)
    03000000030000000300000003000000: ~(Deleted)
Objects:
    03000000030000000300000003000000:
        Name: obj1
        Strings: {}
        Objects: {}
    04000000040000000400000004000000:
        Name: obj2
        Strings: {}
        Objects: {}
    01000000010000000100000001000000: ~(Deleted)
    06000000060000000600000006000000: ~(Deleted)
";

        private const string YamlDictionaryWithDeleted = @"!Stride.Core.Assets.Tests.Yaml.TestCollectionIdsSerialization+ContainerDictionary,Stride.Core.Assets.Tests
Name: Root
Strings:
    08000000080000000800000008000000~000000c8-00c8-0000-c800-0000c8000000: aaa
    05000000050000000500000005000000~00000064-0064-0000-6400-000064000000: bbb
    01000000010000000100000001000000~: ~(Deleted)
    03000000030000000300000003000000~: ~(Deleted)
Objects:
    03000000030000000300000003000000~key3:
        Name: obj1
        Strings: {}
        Objects: {}
    04000000040000000400000004000000~key4:
        Name: obj2
        Strings: {}
        Objects: {}
    01000000010000000100000001000000~: ~(Deleted)
    06000000060000000600000006000000~: ~(Deleted)
";

        private static string SerializeAsString(object instance)
        {
            using (var stream = new MemoryStream())
            {
                AssetYamlSerializer.Default.Serialize(stream, instance);
                stream.Flush();
                stream.Position = 0;
                return new StreamReader(stream).ReadToEnd();
            }
        }

        [Fact]
        public void TestCollectionSerialization()
        {
            ShadowObject.Enable = true;
            var obj = new ContainerCollection("Root")
            {
                Strings = { "aaa", "bbb" },
                Objects = { new ContainerCollection("obj1"), new ContainerCollection("obj2") }
            };

            var stringIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Strings);
            stringIds[0] = IdentifierGenerator.Get(2);
            stringIds[1] = IdentifierGenerator.Get(1);
            var objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            objectIds[0] = IdentifierGenerator.Get(3);
            objectIds[1] = IdentifierGenerator.Get(4);
            var yaml = SerializeAsString(obj);
            Assert.Equal(YamlCollection, yaml);
        }

        [Fact]
        public void TestCollectionDeserialization()
        {
            ShadowObject.Enable = true;
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(YamlCollection);
            writer.Flush();
            stream.Position = 0;
            var instance = AssetYamlSerializer.Default.Deserialize(stream);
            Assert.NotNull(instance);
            Assert.Equal(typeof(ContainerCollection), instance.GetType());
            var obj = (ContainerCollection)instance;
            Assert.Equal("Root", obj.Name);
            Assert.Equal(2, obj.Strings.Count);
            Assert.Equal("aaa", obj.Strings[0]);
            Assert.Equal("bbb", obj.Strings[1]);
            Assert.Equal(2, obj.Objects.Count);
            Assert.Equal("obj1", obj.Objects[0].Name);
            Assert.Equal("obj2", obj.Objects[1].Name);
            var stringIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Strings);
            Assert.Equal(IdentifierGenerator.Get(2), stringIds[0]);
            Assert.Equal(IdentifierGenerator.Get(1), stringIds[1]);
            var objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            Assert.Equal(IdentifierGenerator.Get(3), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(4), objectIds[1]);
        }

        [Fact]
        public void TestDictionarySerialization()
        {
            ShadowObject.Enable = true;
            var obj = new ContainerDictionary("Root")
            {
                Strings = { { GuidGenerator.Get(200), "aaa" }, { GuidGenerator.Get(100), "bbb" } },
                Objects = { { "key3", new ContainerCollection("obj1") }, { "key4", new ContainerCollection("obj2") } },
            };

            var stringIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Strings);
            stringIds[GuidGenerator.Get(200)] = IdentifierGenerator.Get(2);
            stringIds[GuidGenerator.Get(100)] = IdentifierGenerator.Get(1);
            var objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            objectIds["key3"] = IdentifierGenerator.Get(3);
            objectIds["key4"] = IdentifierGenerator.Get(4);
            var yaml = SerializeAsString(obj);
            Assert.Equal(YamlDictionary, yaml);
        }

        [Fact]
        public void TestDictionaryDeserialization()
        {
            ShadowObject.Enable = true;
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(YamlDictionary);
            writer.Flush();
            stream.Position = 0;
            var instance = AssetYamlSerializer.Default.Deserialize(stream);
            Assert.NotNull(instance);
            Assert.Equal(typeof(ContainerDictionary), instance.GetType());
            var obj = (ContainerDictionary)instance;
            Assert.Equal("Root", obj.Name);
            Assert.Equal(2, obj.Strings.Count);
            Assert.Equal("aaa", obj.Strings[GuidGenerator.Get(200)]);
            Assert.Equal("bbb", obj.Strings[GuidGenerator.Get(100)]);
            Assert.Equal(2, obj.Objects.Count);
            Assert.Equal("obj1", obj.Objects["key3"].Name);
            Assert.Equal("obj2", obj.Objects["key4"].Name);
            var stringIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Strings);
            Assert.Equal(IdentifierGenerator.Get(2), stringIds[GuidGenerator.Get(200)]);
            Assert.Equal(IdentifierGenerator.Get(1), stringIds[GuidGenerator.Get(100)]);
            var objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            Assert.Equal(IdentifierGenerator.Get(3), objectIds["key3"]);
            Assert.Equal(IdentifierGenerator.Get(4), objectIds["key4"]);
        }

        [Fact]
        public void TestCollectionDeserializationWithDeleted()
        {
            ShadowObject.Enable = true;
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(YamlCollectionWithDeleted);
            writer.Flush();
            stream.Position = 0;
            var instance = AssetYamlSerializer.Default.Deserialize(stream);
            Assert.NotNull(instance);
            Assert.Equal(typeof(ContainerCollection), instance.GetType());
            var obj = (ContainerCollection)instance;
            Assert.Equal("Root", obj.Name);
            Assert.Equal(2, obj.Strings.Count);
            Assert.Equal("aaa", obj.Strings[0]);
            Assert.Equal("bbb", obj.Strings[1]);
            Assert.Equal(2, obj.Objects.Count);
            Assert.Equal("obj1", obj.Objects[0].Name);
            Assert.Equal("obj2", obj.Objects[1].Name);
            var stringIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Strings);
            Assert.Equal(IdentifierGenerator.Get(8), stringIds[0]);
            Assert.Equal(IdentifierGenerator.Get(5), stringIds[1]);
            var objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            Assert.Equal(IdentifierGenerator.Get(3), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(4), objectIds[1]);
            var deletedItems = stringIds.DeletedItems.ToList();
            Assert.Equal(2, deletedItems.Count);
            Assert.Equal(IdentifierGenerator.Get(1), deletedItems[0]);
            Assert.Equal(IdentifierGenerator.Get(3), deletedItems[1]);
            deletedItems = objectIds.DeletedItems.ToList();
            Assert.Equal(2, deletedItems.Count);
            Assert.Equal(IdentifierGenerator.Get(1), deletedItems[0]);
            Assert.Equal(IdentifierGenerator.Get(6), deletedItems[1]);
        }

        [Fact]
        public void TestCollectionSerializationWithDeleted()
        {
            ShadowObject.Enable = true;
            var obj = new ContainerCollection("Root")
            {
                Strings = { "aaa", "bbb" },
                Objects = { new ContainerCollection("obj1"), new ContainerCollection("obj2") }
            };

            var stringIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Strings);
            stringIds[0] = IdentifierGenerator.Get(8);
            stringIds[1] = IdentifierGenerator.Get(5);
            stringIds.MarkAsDeleted(IdentifierGenerator.Get(3));
            stringIds.MarkAsDeleted(IdentifierGenerator.Get(1));
            var objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            objectIds[0] = IdentifierGenerator.Get(3);
            objectIds[1] = IdentifierGenerator.Get(4);
            objectIds.MarkAsDeleted(IdentifierGenerator.Get(1));
            objectIds.MarkAsDeleted(IdentifierGenerator.Get(6));
            var yaml = SerializeAsString(obj);
            Assert.Equal(YamlCollectionWithDeleted, yaml);
        }

        [Fact]
        public void TestDictionaryDeserializationWithDeleted()
        {
            ShadowObject.Enable = true;
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(YamlDictionaryWithDeleted);
            writer.Flush();
            stream.Position = 0;
            var instance = AssetYamlSerializer.Default.Deserialize(stream);
            Assert.NotNull(instance);
            Assert.Equal(typeof(ContainerDictionary), instance.GetType());
            var obj = (ContainerDictionary)instance;
            Assert.Equal("Root", obj.Name);
            Assert.Equal(2, obj.Strings.Count);
            Assert.Equal("aaa", obj.Strings[GuidGenerator.Get(200)]);
            Assert.Equal("bbb", obj.Strings[GuidGenerator.Get(100)]);
            Assert.Equal(2, obj.Objects.Count);
            Assert.Equal("obj1", obj.Objects["key3"].Name);
            Assert.Equal("obj2", obj.Objects["key4"].Name);
            var stringIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Strings);
            Assert.Equal(IdentifierGenerator.Get(8), stringIds[GuidGenerator.Get(200)]);
            Assert.Equal(IdentifierGenerator.Get(5), stringIds[GuidGenerator.Get(100)]);
            var objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            Assert.Equal(IdentifierGenerator.Get(3), objectIds["key3"]);
            Assert.Equal(IdentifierGenerator.Get(4), objectIds["key4"]);
            var deletedItems = stringIds.DeletedItems.ToList();
            Assert.Equal(2, deletedItems.Count);
            Assert.Equal(IdentifierGenerator.Get(1), deletedItems[0]);
            Assert.Equal(IdentifierGenerator.Get(3), deletedItems[1]);
            deletedItems = objectIds.DeletedItems.ToList();
            Assert.Equal(2, deletedItems.Count);
            Assert.Equal(IdentifierGenerator.Get(1), deletedItems[0]);
            Assert.Equal(IdentifierGenerator.Get(6), deletedItems[1]);
        }

        [Fact]
        public void TestDictionarySerializationWithDeleted()
        {
            ShadowObject.Enable = true;
            var obj = new ContainerDictionary("Root")
            {
                Strings = { { GuidGenerator.Get(200), "aaa" }, { GuidGenerator.Get(100), "bbb" } },
                Objects = { { "key3", new ContainerCollection("obj1") }, { "key4", new ContainerCollection("obj2") } },
            };

            var stringIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Strings);
            stringIds[GuidGenerator.Get(200)] = IdentifierGenerator.Get(8);
            stringIds[GuidGenerator.Get(100)] = IdentifierGenerator.Get(5);
            stringIds.MarkAsDeleted(IdentifierGenerator.Get(3));
            stringIds.MarkAsDeleted(IdentifierGenerator.Get(1));
            var objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            objectIds["key3"] = IdentifierGenerator.Get(3);
            objectIds["key4"] = IdentifierGenerator.Get(4);
            objectIds.MarkAsDeleted(IdentifierGenerator.Get(1));
            objectIds.MarkAsDeleted(IdentifierGenerator.Get(6));
            var yaml = SerializeAsString(obj);
            Assert.Equal(YamlDictionaryWithDeleted, yaml);
        }

        [Fact]
        public void TestCollectionNonIdentifiableItemsSerialization()
        {
            ShadowObject.Enable = true;
            var obj = new ContainerNonIdentifiableCollection("Root")
            {
                Objects = { new ContainerCollection { Name = "aaa", Strings = { "bbb", "ccc" } }, new ContainerCollection { Name = "ddd", Strings = { "eee", "fff" } } },
                NonIdentifiableObjects = { new ContainerCollection { Name = "ggg", Strings = { "hhh", "iii" } }, new ContainerCollection { Name = "jjj", Strings = { "kkk", "lll" } } },
            };

            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            ids[0] = IdentifierGenerator.Get(2);
            ids[1] = IdentifierGenerator.Get(1);
            ids = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects[0].Strings);
            ids[0] = IdentifierGenerator.Get(5);
            ids[1] = IdentifierGenerator.Get(6);
            ids = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects[1].Strings);
            ids[0] = IdentifierGenerator.Get(7);
            ids[1] = IdentifierGenerator.Get(8);
            ids = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects);
            ids[0] = IdentifierGenerator.Get(3);
            ids[1] = IdentifierGenerator.Get(4);
            ids = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects[0].Strings);
            ids[0] = IdentifierGenerator.Get(9);
            ids[1] = IdentifierGenerator.Get(10);
            ids = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects[1].Strings);
            ids[0] = IdentifierGenerator.Get(11);
            ids[1] = IdentifierGenerator.Get(12);
            var yaml = SerializeAsString(obj);
            Assert.Equal(YamlCollectionNotIdentifiable, yaml);
        }

        [Fact]
        public void TestCollectionNonIdentifiableItemsDeserialization()
        {
            ShadowObject.Enable = true;
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(YamlCollectionNotIdentifiable);
            writer.Flush();
            stream.Position = 0;
            var instance = AssetYamlSerializer.Default.Deserialize(stream);
            Assert.NotNull(instance);
            Assert.Equal(typeof(ContainerNonIdentifiableCollection), instance.GetType());
            var obj = (ContainerNonIdentifiableCollection)instance;
            Assert.Equal("Root", obj.Name);
            Assert.Equal(2, obj.Objects.Count);
            Assert.Equal("aaa", obj.Objects[0].Name);
            Assert.Equal(2, obj.Objects[0].Strings.Count);
            Assert.Equal("bbb", obj.Objects[0].Strings[0]);
            Assert.Equal("ccc", obj.Objects[0].Strings[1]);
            Assert.Equal("ddd", obj.Objects[1].Name);
            Assert.Equal(2, obj.Objects[1].Strings.Count);
            Assert.Equal("eee", obj.Objects[1].Strings[0]);
            Assert.Equal("fff", obj.Objects[1].Strings[1]);
            var objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            Assert.Equal(IdentifierGenerator.Get(2), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(1), objectIds[1]);
            objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects[0].Strings);
            Assert.Equal(IdentifierGenerator.Get(5), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(6), objectIds[1]);
            objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects[1].Strings);
            Assert.Equal(IdentifierGenerator.Get(7), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(8), objectIds[1]);

            Assert.Equal(2, obj.NonIdentifiableObjects.Count);
            Assert.Equal("ggg", obj.NonIdentifiableObjects[0].Name);
            Assert.Equal(2, obj.NonIdentifiableObjects[0].Strings.Count);
            Assert.Equal("hhh", obj.NonIdentifiableObjects[0].Strings[0]);
            Assert.Equal("iii", obj.NonIdentifiableObjects[0].Strings[1]);
            Assert.Equal("jjj", obj.NonIdentifiableObjects[1].Name);
            Assert.Equal(2, obj.NonIdentifiableObjects[1].Strings.Count);
            Assert.Equal("kkk", obj.NonIdentifiableObjects[1].Strings[0]);
            Assert.Equal("lll", obj.NonIdentifiableObjects[1].Strings[1]);
            Assert.False(CollectionItemIdHelper.TryGetCollectionItemIds(obj.NonIdentifiableObjects, out objectIds));
            objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects);
            Assert.Equal(0, objectIds.KeyCount);
            Assert.Equal(0, objectIds.DeletedCount);
            objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects[0].Strings);
            Assert.Equal(IdentifierGenerator.Get(9), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(10), objectIds[1]);
            objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects[1].Strings);
            Assert.Equal(IdentifierGenerator.Get(11), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(12), objectIds[1]);
        }

        [Fact]
        public void TestDictionaryNonIdentifiableItemsSerialization()
        {
            ShadowObject.Enable = true;
            var obj = new ContainerNonIdentifiableDictionary("Root")
            {
                Objects = { { "AAA", new ContainerCollection { Name = "aaa", Strings = { "bbb", "ccc" } } }, { "BBB", new ContainerCollection { Name = "ddd", Strings = { "eee", "fff" } } } },
                NonIdentifiableObjects = { { "CCC", new ContainerCollection { Name = "ggg", Strings = { "hhh", "iii" } } }, { "DDD", new ContainerCollection { Name = "jjj", Strings = { "kkk", "lll" } } } },
            };

            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            ids["AAA"] = IdentifierGenerator.Get(2);
            ids["BBB"] = IdentifierGenerator.Get(1);
            ids = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects["AAA"].Strings);
            ids[0] = IdentifierGenerator.Get(5);
            ids[1] = IdentifierGenerator.Get(6);
            ids = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects["BBB"].Strings);
            ids[0] = IdentifierGenerator.Get(7);
            ids[1] = IdentifierGenerator.Get(8);
            ids = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects);
            ids["CCC"] = IdentifierGenerator.Get(3);
            ids["DDD"] = IdentifierGenerator.Get(4);
            ids = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects["CCC"].Strings);
            ids[0] = IdentifierGenerator.Get(9);
            ids[1] = IdentifierGenerator.Get(10);
            ids = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects["DDD"].Strings);
            ids[0] = IdentifierGenerator.Get(11);
            ids[1] = IdentifierGenerator.Get(12);
            var yaml = SerializeAsString(obj);
            Assert.Equal(YamlDictionaryNonIdentifiable, yaml);
        }

        [Fact]
        public void TestDictionaryNonIdentifiableItemsDeserialization()
        {
            ShadowObject.Enable = true;
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(YamlDictionaryNonIdentifiable);
            writer.Flush();
            stream.Position = 0;
            var instance = AssetYamlSerializer.Default.Deserialize(stream);
            Assert.NotNull(instance);
            Assert.Equal(typeof(ContainerNonIdentifiableDictionary), instance.GetType());
            var obj = (ContainerNonIdentifiableDictionary)instance;
            Assert.Equal("Root", obj.Name);
            Assert.Equal(2, obj.Objects.Count);
            Assert.Equal("aaa", obj.Objects["AAA"].Name);
            Assert.Equal(2, obj.Objects["AAA"].Strings.Count);
            Assert.Equal("bbb", obj.Objects["AAA"].Strings[0]);
            Assert.Equal("ccc", obj.Objects["AAA"].Strings[1]);
            Assert.Equal("ddd", obj.Objects["BBB"].Name);
            Assert.Equal(2, obj.Objects["BBB"].Strings.Count);
            Assert.Equal("eee", obj.Objects["BBB"].Strings[0]);
            Assert.Equal("fff", obj.Objects["BBB"].Strings[1]);
            var objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects);
            Assert.Equal(IdentifierGenerator.Get(2), objectIds["AAA"]);
            Assert.Equal(IdentifierGenerator.Get(1), objectIds["BBB"]);
            objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects["AAA"].Strings);
            Assert.Equal(IdentifierGenerator.Get(5), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(6), objectIds[1]);
            objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.Objects["BBB"].Strings);
            Assert.Equal(IdentifierGenerator.Get(7), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(8), objectIds[1]);

            Assert.Equal(2, obj.NonIdentifiableObjects.Count);
            Assert.Equal("ggg", obj.NonIdentifiableObjects["CCC"].Name);
            Assert.Equal(2, obj.NonIdentifiableObjects["CCC"].Strings.Count);
            Assert.Equal("hhh", obj.NonIdentifiableObjects["CCC"].Strings[0]);
            Assert.Equal("iii", obj.NonIdentifiableObjects["CCC"].Strings[1]);
            Assert.Equal("jjj", obj.NonIdentifiableObjects["DDD"].Name);
            Assert.Equal(2, obj.NonIdentifiableObjects["DDD"].Strings.Count);
            Assert.Equal("kkk", obj.NonIdentifiableObjects["DDD"].Strings[0]);
            Assert.Equal("lll", obj.NonIdentifiableObjects["DDD"].Strings[1]);
            Assert.False(CollectionItemIdHelper.TryGetCollectionItemIds(obj.NonIdentifiableObjects, out objectIds));
            objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects);
            Assert.Equal(0, objectIds.KeyCount);
            Assert.Equal(0, objectIds.DeletedCount);
            objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects["CCC"].Strings);
            Assert.Equal(IdentifierGenerator.Get(9), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(10), objectIds[1]);
            objectIds = CollectionItemIdHelper.GetCollectionItemIds(obj.NonIdentifiableObjects["DDD"].Strings);
            Assert.Equal(IdentifierGenerator.Get(11), objectIds[0]);
            Assert.Equal(IdentifierGenerator.Get(12), objectIds[1]);
        }

        [Fact]
        public void TestIdsGeneration()
        {
            ShadowObject.Enable = true;
            CollectionItemIdentifiers ids;

            var obj1 = new ContainerCollection("Root")
            {
                Strings = { "aaa", "bbb", "ccc" },
                Objects = { new ContainerCollection("obj1"), new ContainerCollection("obj2") }
            };
            var hashSet = new HashSet<ItemId>();
            Assert.False(CollectionItemIdHelper.TryGetCollectionItemIds(obj1.Strings, out ids));
            Assert.False(CollectionItemIdHelper.TryGetCollectionItemIds(obj1.Strings, out ids));
            Assert.False(CollectionItemIdHelper.TryGetCollectionItemIds(obj1.Objects, out ids));
            Assert.False(CollectionItemIdHelper.TryGetCollectionItemIds(obj1.Objects, out ids));
            AssetCollectionItemIdHelper.GenerateMissingItemIds(obj1);
            Assert.True(CollectionItemIdHelper.TryGetCollectionItemIds(obj1.Strings, out ids));
            Assert.Equal(3, ids.KeyCount);
            Assert.Equal(0, ids.DeletedCount);
            Assert.True(ids.ContainsKey(0));
            Assert.True(ids.ContainsKey(1));
            Assert.True(ids.ContainsKey(2));
            hashSet.Add(ids[0]);
            hashSet.Add(ids[1]);
            hashSet.Add(ids[2]);
            Assert.True(CollectionItemIdHelper.TryGetCollectionItemIds(obj1.Objects, out ids));
            Assert.Equal(2, ids.KeyCount);
            Assert.Equal(0, ids.DeletedCount);
            Assert.True(ids.ContainsKey(0));
            Assert.True(ids.ContainsKey(1));
            hashSet.Add(ids[0]);
            hashSet.Add(ids[1]);
            Assert.Equal(5, hashSet.Count);

            var obj2 = new ContainerDictionary("Root")
            {
                Strings = { { GuidGenerator.Get(200), "aaa" }, { GuidGenerator.Get(100), "bbb" }, { GuidGenerator.Get(300), "ccc" } },
                Objects = { { "key3", new ContainerCollection("obj1") }, { "key4", new ContainerCollection("obj2") } },
            };
            hashSet.Clear();
            Assert.False(CollectionItemIdHelper.TryGetCollectionItemIds(obj2.Strings, out ids));
            Assert.False(CollectionItemIdHelper.TryGetCollectionItemIds(obj2.Strings, out ids));
            Assert.False(CollectionItemIdHelper.TryGetCollectionItemIds(obj2.Objects, out ids));
            Assert.False(CollectionItemIdHelper.TryGetCollectionItemIds(obj2.Objects, out ids));
            AssetCollectionItemIdHelper.GenerateMissingItemIds(obj2);
            Assert.True(CollectionItemIdHelper.TryGetCollectionItemIds(obj2.Strings, out ids));
            Assert.Equal(3, ids.KeyCount);
            Assert.Equal(0, ids.DeletedCount);
            Assert.True(ids.ContainsKey(GuidGenerator.Get(200)));
            Assert.True(ids.ContainsKey(GuidGenerator.Get(100)));
            Assert.True(ids.ContainsKey(GuidGenerator.Get(300)));
            hashSet.Add(ids[GuidGenerator.Get(200)]);
            hashSet.Add(ids[GuidGenerator.Get(100)]);
            hashSet.Add(ids[GuidGenerator.Get(300)]);
            Assert.True(CollectionItemIdHelper.TryGetCollectionItemIds(obj2.Objects, out ids));
            Assert.Equal(2, ids.KeyCount);
            Assert.Equal(0, ids.DeletedCount);
            Assert.True(ids.ContainsKey("key3"));
            Assert.True(ids.ContainsKey("key4"));
            hashSet.Add(ids["key3"]);
            hashSet.Add(ids["key4"]);
            Assert.Equal(5, hashSet.Count);
        }
    }
}
