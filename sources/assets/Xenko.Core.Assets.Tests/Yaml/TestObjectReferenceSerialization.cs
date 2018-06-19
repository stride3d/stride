// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xenko.Core.Assets.Serializers;
using Xenko.Core.Assets.Tests.Helpers;
using Xenko.Core.Assets.Yaml;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml;

namespace Xenko.Core.Assets.Tests.Yaml
{
    [TestFixture]
    public class TestObjectReferenceSerialization
    {
        public interface IReferenceable : IIdentifiable
        {
            string Value { get; set; }
        }

        public class Referenceable : IReferenceable
        {
            [NonOverridable]
            public Guid Id { get; set; }

            public string Value { get; set; }
        }

        public class CollectionContainer
        {
            public List<Referenceable> ConcreteRefList { get; set; } = new List<Referenceable>();
            public List<IReferenceable> AbstractRefList { get; set; } = new List<IReferenceable>();
            public Dictionary<string, Referenceable> ConcreteRefDictionary { get; set; } = new Dictionary<string, Referenceable>();
            public Dictionary<string, IReferenceable> AbstractRefDictionary { get; set; } = new Dictionary<string, IReferenceable>();
        }

        public class NonIdentifiableCollectionContainer
        {
            [NonIdentifiableCollectionItems]
            public List<Referenceable> ConcreteRefList { get; set; } = new List<Referenceable>();
            [NonIdentifiableCollectionItems]
            public List<IReferenceable> AbstractRefList { get; set; } = new List<IReferenceable>();
            [NonIdentifiableCollectionItems]
            public Dictionary<string, Referenceable> ConcreteRefDictionary { get; set; } = new Dictionary<string, Referenceable>();
            [NonIdentifiableCollectionItems]
            public Dictionary<string, IReferenceable> AbstractRefDictionary { get; set; } = new Dictionary<string, IReferenceable>();
        }

        public class Container
        {
            public Referenceable Referenceable1 { get; set; }
            public Referenceable Referenceable2 { get; set; }            
            public IReferenceable Referenceable3 { get; set; }            
            public IReferenceable Referenceable4 { get; set; }            
        }

        private const string ExpandedObjectYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Container,Xenko.Core.Assets.Tests
Referenceable1:
    Id: 00000001-0001-0000-0100-000001000000
    Value: Test
Referenceable2: null
Referenceable3: null
Referenceable4: null
";

        private const string ConcreteReferenceConcreteObjectYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Container,Xenko.Core.Assets.Tests
Referenceable1:
    Id: 00000001-0001-0000-0100-000001000000
    Value: Test
Referenceable2: ref!! 00000001-0001-0000-0100-000001000000
Referenceable3: null
Referenceable4: null
";

        private const string AbstractReferenceConcreteObjectYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Container,Xenko.Core.Assets.Tests
Referenceable1:
    Id: 00000001-0001-0000-0100-000001000000
    Value: Test
Referenceable2: null
Referenceable3: !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests ref!! 00000001-0001-0000-0100-000001000000
Referenceable4: null
";

        private const string ConcreteReferenceAbstractObjectYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Container,Xenko.Core.Assets.Tests
Referenceable1: null
Referenceable2: ref!! 00000001-0001-0000-0100-000001000000
Referenceable3: !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests
    Id: 00000001-0001-0000-0100-000001000000
    Value: Test
Referenceable4: null
";

        private const string AbstractReferenceAbstractObjectYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Container,Xenko.Core.Assets.Tests
Referenceable1: null
Referenceable2: null
Referenceable3: !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests
    Id: 00000001-0001-0000-0100-000001000000
    Value: Test
Referenceable4: !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests ref!! 00000001-0001-0000-0100-000001000000
";

        private const string ConcreteReferenceableListYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+CollectionContainer,Xenko.Core.Assets.Tests
ConcreteRefList:
    01000000010000000100000001000000: ref!! 00000001-0001-0000-0100-000001000000
    02000000020000000200000002000000:
        Id: 00000001-0001-0000-0100-000001000000
        Value: Test
AbstractRefList: {}
ConcreteRefDictionary: {}
AbstractRefDictionary: {}
";

        private const string AbstractReferenceableListYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+CollectionContainer,Xenko.Core.Assets.Tests
ConcreteRefList: {}
AbstractRefList:
    01000000010000000100000001000000: !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests ref!! 00000001-0001-0000-0100-000001000000
    02000000020000000200000002000000: !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests
        Id: 00000001-0001-0000-0100-000001000000
        Value: Test
ConcreteRefDictionary: {}
AbstractRefDictionary: {}
";

        private const string ConcreteReferenceableDictionaryYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+CollectionContainer,Xenko.Core.Assets.Tests
ConcreteRefList: {}
AbstractRefList: {}
ConcreteRefDictionary:
    01000000010000000100000001000000~Item1: ref!! 00000001-0001-0000-0100-000001000000
    02000000020000000200000002000000~Item2:
        Id: 00000001-0001-0000-0100-000001000000
        Value: Test
AbstractRefDictionary: {}
";

        private const string AbstractReferenceableDictionaryYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+CollectionContainer,Xenko.Core.Assets.Tests
ConcreteRefList: {}
AbstractRefList: {}
ConcreteRefDictionary: {}
AbstractRefDictionary:
    01000000010000000100000001000000~Item1: !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests ref!! 00000001-0001-0000-0100-000001000000
    02000000020000000200000002000000~Item2: !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests
        Id: 00000001-0001-0000-0100-000001000000
        Value: Test
";

        private const string ConcreteNonIdentifiableReferenceableListYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+NonIdentifiableCollectionContainer,Xenko.Core.Assets.Tests
ConcreteRefList:
    - ref!! 00000001-0001-0000-0100-000001000000
    -   Id: 00000001-0001-0000-0100-000001000000
        Value: Test
AbstractRefList: []
ConcreteRefDictionary: {}
AbstractRefDictionary: {}
";

        private const string AbstractNonIdentifiableReferenceableListYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+NonIdentifiableCollectionContainer,Xenko.Core.Assets.Tests
ConcreteRefList: []
AbstractRefList:
    - !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests ref!! 00000001-0001-0000-0100-000001000000
    - !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests
        Id: 00000001-0001-0000-0100-000001000000
        Value: Test
ConcreteRefDictionary: {}
AbstractRefDictionary: {}
";

        private const string ConcreteNonIdentifiableReferenceableDictionaryYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+NonIdentifiableCollectionContainer,Xenko.Core.Assets.Tests
ConcreteRefList: []
AbstractRefList: []
ConcreteRefDictionary:
    Item1: ref!! 00000001-0001-0000-0100-000001000000
    Item2:
        Id: 00000001-0001-0000-0100-000001000000
        Value: Test
AbstractRefDictionary: {}
";

        private const string AbstractNonIdentifiableReferenceableDictionaryYaml = @"!Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+NonIdentifiableCollectionContainer,Xenko.Core.Assets.Tests
ConcreteRefList: []
AbstractRefList: []
ConcreteRefDictionary: {}
AbstractRefDictionary:
    Item1: !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests ref!! 00000001-0001-0000-0100-000001000000
    Item2: !Xenko.Core.Assets.Tests.Yaml.TestObjectReferenceSerialization+Referenceable,Xenko.Core.Assets.Tests
        Id: 00000001-0001-0000-0100-000001000000
        Value: Test
";

        [Test]
        public void TestExpandObjectSerialization()
        {
            var obj = new Container { Referenceable1 = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" } };
            var yaml = SerializeAsString(obj, null);
            Assert.AreEqual(ExpandedObjectYaml, yaml);
            yaml = SerializeAsString(obj, new YamlAssetMetadata<Guid>());
            Assert.AreEqual(ExpandedObjectYaml, yaml);
        }

        [Test]
        public void TestExpandObjectDeserialization()
        {
            var obj = (Container)Deserialize(ExpandedObjectYaml);
            Assert.NotNull(obj.Referenceable1);
            Assert.Null(obj.Referenceable2);
            Assert.Null(obj.Referenceable3);
            Assert.Null(obj.Referenceable4);
            Assert.AreEqual(GuidGenerator.Get(1), obj.Referenceable1.Id);
            Assert.AreEqual("Test", obj.Referenceable1.Value);
        }

        [Test]
        public void TestConcreteReferenceConcreteObjectSerialization()
        {
            var obj = new Container { Referenceable1 = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" } };
            obj.Referenceable2 = obj.Referenceable1;
            var objectReferences = new YamlAssetMetadata<Guid>();
            var path = new YamlAssetPath();
            path.PushMember(nameof(Container.Referenceable2));
            objectReferences.Set(path, obj.Referenceable2.Id);
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(ConcreteReferenceConcreteObjectYaml, yaml);
        }

        [Test]
        public void TestConcreteReferenceConcreteObjectDeserialization()
        {
            var obj = (Container)Deserialize(ConcreteReferenceConcreteObjectYaml);
            Assert.NotNull(obj.Referenceable1);
            Assert.NotNull(obj.Referenceable2);
            Assert.AreEqual(obj.Referenceable1, obj.Referenceable2);
            Assert.Null(obj.Referenceable3);
            Assert.Null(obj.Referenceable4);
            Assert.AreEqual(GuidGenerator.Get(1), obj.Referenceable1.Id);
            Assert.AreEqual("Test", obj.Referenceable1.Value);
        }

        [Test]
        public void TestAbstractReferenceConcreteObjectSerialization()
        {
            var obj = new Container { Referenceable1 = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" } };
            obj.Referenceable3 = obj.Referenceable1;
            var objectReferences = new YamlAssetMetadata<Guid>();
            var path = new YamlAssetPath();
            path.PushMember(nameof(Container.Referenceable3));
            objectReferences.Set(path, obj.Referenceable3.Id);
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(AbstractReferenceConcreteObjectYaml, yaml);
        }

        [Test]
        public void TestAbstractReferenceConcreteObjectDeserialization()
        {
            var obj = (Container)Deserialize(AbstractReferenceConcreteObjectYaml);
            Assert.NotNull(obj.Referenceable1);
            Assert.NotNull(obj.Referenceable3);
            Assert.AreEqual(obj.Referenceable1, obj.Referenceable3);
            Assert.Null(obj.Referenceable2);
            Assert.Null(obj.Referenceable4);
            Assert.AreEqual(GuidGenerator.Get(1), obj.Referenceable1.Id);
            Assert.AreEqual("Test", obj.Referenceable1.Value);
        }

        [Test]
        public void TestConcreteReferenceObjectAbstractSerialization()
        {
            var obj = new Container { Referenceable3 = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" } };
            obj.Referenceable2 = (Referenceable)obj.Referenceable3;
            var objectReferences = new YamlAssetMetadata<Guid>();
            var path = new YamlAssetPath();
            path.PushMember(nameof(Container.Referenceable2));
            objectReferences.Set(path, obj.Referenceable2.Id);
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(ConcreteReferenceAbstractObjectYaml, yaml);
        }

        [Test]
        public void TestConcreteReferenceObjectAbstractDeserialization()
        {
            var obj = (Container)Deserialize(ConcreteReferenceAbstractObjectYaml);
            Assert.NotNull(obj.Referenceable2);
            Assert.NotNull(obj.Referenceable3);
            Assert.AreEqual(obj.Referenceable2, obj.Referenceable3);
            Assert.Null(obj.Referenceable1);
            Assert.Null(obj.Referenceable4);
            Assert.AreEqual(GuidGenerator.Get(1), obj.Referenceable2.Id);
            Assert.AreEqual("Test", obj.Referenceable2.Value);
        }

        [Test]
        public void TestAbstracteferenceObjectAbstractSerialization()
        {
            var obj = new Container { Referenceable3 = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" } };
            obj.Referenceable4 = (Referenceable)obj.Referenceable3;
            var objectReferences = new YamlAssetMetadata<Guid>();
            var path = new YamlAssetPath();
            path.PushMember(nameof(Container.Referenceable4));
            objectReferences.Set(path, obj.Referenceable4.Id);
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(AbstractReferenceAbstractObjectYaml, yaml);
        }

        [Test]
        public void TestAbstracteferenceObjectAbstractDeserialization()
        {
            var obj = (Container)Deserialize(AbstractReferenceAbstractObjectYaml);
            Assert.NotNull(obj.Referenceable3);
            Assert.NotNull(obj.Referenceable4);
            Assert.AreEqual(obj.Referenceable3, obj.Referenceable4);
            Assert.Null(obj.Referenceable1);
            Assert.Null(obj.Referenceable2);
            Assert.AreEqual(GuidGenerator.Get(1), obj.Referenceable3.Id);
            Assert.AreEqual("Test", obj.Referenceable3.Value);
        }

        [Test]
        public void TestConcreteReferenceableListSerialization()
        {
            var obj = new CollectionContainer();
            var item = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" };
            obj.ConcreteRefList.Add(item);
            obj.ConcreteRefList.Add(item);
            var objectReferences = new YamlAssetMetadata<Guid>();
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.ConcreteRefList);
            ids[0] = IdentifierGenerator.Get(1);
            ids[1] = IdentifierGenerator.Get(2);
            var path = new YamlAssetPath();
            path.PushMember(nameof(CollectionContainer.ConcreteRefList));
            path.PushItemId(IdentifierGenerator.Get(1));
            objectReferences.Set(path, GuidGenerator.Get(1));
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(ConcreteReferenceableListYaml, yaml);
        }

        [Test]
        public void TestConcreteReferenceableListDeserialization()
        {
            var obj = (CollectionContainer)Deserialize(ConcreteReferenceableListYaml);
            Assert.NotNull(obj.ConcreteRefList);
            Assert.AreEqual(2, obj.ConcreteRefList.Count);
            Assert.AreEqual(obj.ConcreteRefList[0], obj.ConcreteRefList[1]);
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.ConcreteRefList);
            Assert.AreEqual(IdentifierGenerator.Get(1), ids[0]);
            Assert.AreEqual(IdentifierGenerator.Get(2), ids[1]);
            Assert.AreEqual(GuidGenerator.Get(1), obj.ConcreteRefList[0].Id);
            Assert.AreEqual("Test", obj.ConcreteRefList[0].Value);
        }

        [Test]
        public void TestAbstractReferenceableListSerialization()
        {
            var obj = new CollectionContainer();
            var item = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" };
            obj.AbstractRefList.Add(item);
            obj.AbstractRefList.Add(item);
            var objectReferences = new YamlAssetMetadata<Guid>();
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.AbstractRefList);
            ids[0] = IdentifierGenerator.Get(1);
            ids[1] = IdentifierGenerator.Get(2);
            var path = new YamlAssetPath();
            path.PushMember(nameof(CollectionContainer.AbstractRefList));
            path.PushItemId(IdentifierGenerator.Get(1));
            objectReferences.Set(path, GuidGenerator.Get(1));
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(AbstractReferenceableListYaml, yaml);
        }

        [Test]
        public void TestAbstractReferenceableListDeserialization()
        {
            var obj = (CollectionContainer)Deserialize(AbstractReferenceableListYaml);
            Assert.NotNull(obj.AbstractRefList);
            Assert.AreEqual(2, obj.AbstractRefList.Count);
            Assert.AreEqual(obj.AbstractRefList[0], obj.AbstractRefList[1]);
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.AbstractRefList);
            Assert.AreEqual(IdentifierGenerator.Get(1), ids[0]);
            Assert.AreEqual(IdentifierGenerator.Get(2), ids[1]);
            Assert.AreEqual(GuidGenerator.Get(1), obj.AbstractRefList[0].Id);
            Assert.AreEqual("Test", obj.AbstractRefList[0].Value);
        }

        [Test]
        public void TestConcreteReferenceableDictionarySerialization()
        {
            var obj = new CollectionContainer();
            var item = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" };
            obj.ConcreteRefDictionary.Add("Item1", item);
            obj.ConcreteRefDictionary.Add("Item2", item);
            var objectReferences = new YamlAssetMetadata<Guid>();
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.ConcreteRefDictionary);
            ids["Item1"] = IdentifierGenerator.Get(1);
            ids["Item2"] = IdentifierGenerator.Get(2);
            var path = new YamlAssetPath();
            path.PushMember(nameof(CollectionContainer.ConcreteRefDictionary));
            path.PushItemId(IdentifierGenerator.Get(1));
            objectReferences.Set(path, GuidGenerator.Get(1));
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(ConcreteReferenceableDictionaryYaml, yaml);
        }

        [Test]
        public void TestConcreteReferenceableDictionaryDeserialization()
        {
            var obj = (CollectionContainer)Deserialize(ConcreteReferenceableDictionaryYaml);
            Assert.NotNull(obj.ConcreteRefDictionary);
            Assert.AreEqual(2, obj.ConcreteRefDictionary.Count);
            Assert.AreEqual(obj.ConcreteRefDictionary["Item1"], obj.ConcreteRefDictionary["Item2"]);
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.ConcreteRefDictionary);
            Assert.AreEqual(IdentifierGenerator.Get(1), ids["Item1"]);
            Assert.AreEqual(IdentifierGenerator.Get(2), ids["Item2"]);
            Assert.AreEqual(GuidGenerator.Get(1), obj.ConcreteRefDictionary["Item1"].Id);
            Assert.AreEqual("Test", obj.ConcreteRefDictionary["Item1"].Value);
        }

        [Test]
        public void TestAbstractReferenceableDictionarySerialization()
        {
            var obj = new CollectionContainer();
            var item = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" };
            obj.AbstractRefDictionary.Add("Item1", item);
            obj.AbstractRefDictionary.Add("Item2", item);
            var objectReferences = new YamlAssetMetadata<Guid>();
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.AbstractRefDictionary);
            ids["Item1"] = IdentifierGenerator.Get(1);
            ids["Item2"] = IdentifierGenerator.Get(2);
            var path = new YamlAssetPath();
            path.PushMember(nameof(CollectionContainer.AbstractRefDictionary));
            path.PushItemId(IdentifierGenerator.Get(1));
            objectReferences.Set(path, GuidGenerator.Get(1));
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(AbstractReferenceableDictionaryYaml, yaml);
        }

        [Test]
        public void TestAbstractReferenceableDictionaryDeserialization()
        {
            var obj = (CollectionContainer)Deserialize(AbstractReferenceableDictionaryYaml);
            Assert.NotNull(obj.AbstractRefDictionary);
            Assert.AreEqual(2, obj.AbstractRefDictionary.Count);
            Assert.AreEqual(obj.AbstractRefDictionary["Item1"], obj.AbstractRefDictionary["Item2"]);
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.AbstractRefDictionary);
            Assert.AreEqual(IdentifierGenerator.Get(1), ids["Item1"]);
            Assert.AreEqual(IdentifierGenerator.Get(2), ids["Item2"]);
            Assert.AreEqual(GuidGenerator.Get(1), obj.AbstractRefDictionary["Item1"].Id);
            Assert.AreEqual("Test", obj.AbstractRefDictionary["Item1"].Value);
        }

        [Test]
        public void TestConcreteNonIdentifiableReferenceableListSerialization()
        {
            var obj = new NonIdentifiableCollectionContainer();
            var item = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" };
            obj.ConcreteRefList.Add(item);
            obj.ConcreteRefList.Add(item);
            var objectReferences = new YamlAssetMetadata<Guid>();
            var path = new YamlAssetPath();
            path.PushMember(nameof(CollectionContainer.ConcreteRefList));
            path.PushIndex(0);
            objectReferences.Set(path, GuidGenerator.Get(1));
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(ConcreteNonIdentifiableReferenceableListYaml, yaml);
        }

        [Test]
        public void TestConcreteNonIdentifiableReferenceableListDeserialization()
        {
            var obj = (NonIdentifiableCollectionContainer)Deserialize(ConcreteNonIdentifiableReferenceableListYaml);
            Assert.NotNull(obj.ConcreteRefList);
            Assert.AreEqual(2, obj.ConcreteRefList.Count);
            Assert.AreEqual(obj.ConcreteRefList[0], obj.ConcreteRefList[1]);
            Assert.AreEqual(GuidGenerator.Get(1), obj.ConcreteRefList[0].Id);
            Assert.AreEqual("Test", obj.ConcreteRefList[0].Value);
        }

        [Test]
        public void TestAbstractNonIdentifiableReferenceableListSerialization()
        {
            var obj = new NonIdentifiableCollectionContainer();
            var item = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" };
            obj.AbstractRefList.Add(item);
            obj.AbstractRefList.Add(item);
            var objectReferences = new YamlAssetMetadata<Guid>();
            var path = new YamlAssetPath();
            path.PushMember(nameof(CollectionContainer.AbstractRefList));
            path.PushIndex(0);
            objectReferences.Set(path, GuidGenerator.Get(1));
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(AbstractNonIdentifiableReferenceableListYaml, yaml);
        }

        [Test]
        public void TestAbstractNonIdentifiableReferenceableListDeserialization()
        {
            var obj = (NonIdentifiableCollectionContainer)Deserialize(AbstractNonIdentifiableReferenceableListYaml);
            Assert.NotNull(obj.AbstractRefList);
            Assert.AreEqual(2, obj.AbstractRefList.Count);
            Assert.AreEqual(obj.AbstractRefList[0], obj.AbstractRefList[1]);
            Assert.AreEqual(GuidGenerator.Get(1), obj.AbstractRefList[0].Id);
            Assert.AreEqual("Test", obj.AbstractRefList[0].Value);
        }

        [Test]
        public void TestConcreteNonIdentifiableReferenceableDictionarySerialization()
        {
            var obj = new NonIdentifiableCollectionContainer();
            var item = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" };
            obj.ConcreteRefDictionary.Add("Item1", item);
            obj.ConcreteRefDictionary.Add("Item2", item);
            var objectReferences = new YamlAssetMetadata<Guid>();
            var ids = CollectionItemIdHelper.GetCollectionItemIds(obj.ConcreteRefDictionary);
            ids["Item1"] = IdentifierGenerator.Get(1);
            ids["Item2"] = IdentifierGenerator.Get(2);
            var path = new YamlAssetPath();
            path.PushMember(nameof(CollectionContainer.ConcreteRefDictionary));
            path.PushIndex("Item1");
            objectReferences.Set(path, GuidGenerator.Get(1));
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(ConcreteNonIdentifiableReferenceableDictionaryYaml, yaml);
        }

        [Test]
        public void TestConcreteNonIdentifiableReferenceableDictionaryDeserialization()
        {
            var obj = (NonIdentifiableCollectionContainer)Deserialize(ConcreteNonIdentifiableReferenceableDictionaryYaml);
            Assert.NotNull(obj.ConcreteRefDictionary);
            Assert.AreEqual(2, obj.ConcreteRefDictionary.Count);
            Assert.AreEqual(obj.ConcreteRefDictionary["Item1"], obj.ConcreteRefDictionary["Item2"]);
            Assert.AreEqual(GuidGenerator.Get(1), obj.ConcreteRefDictionary["Item1"].Id);
            Assert.AreEqual("Test", obj.ConcreteRefDictionary["Item1"].Value);
        }

        [Test]
        public void TestAbstractNonIdentifiableReferenceableDictionarySerialization()
        {
            var obj = new NonIdentifiableCollectionContainer();
            var item = new Referenceable { Id = GuidGenerator.Get(1), Value = "Test" };
            obj.AbstractRefDictionary.Add("Item1", item);
            obj.AbstractRefDictionary.Add("Item2", item);
            var objectReferences = new YamlAssetMetadata<Guid>();
            var path = new YamlAssetPath();
            path.PushMember(nameof(CollectionContainer.AbstractRefDictionary));
            path.PushIndex("Item1");
            objectReferences.Set(path, GuidGenerator.Get(1));
            var yaml = SerializeAsString(obj, objectReferences);
            Assert.AreEqual(AbstractNonIdentifiableReferenceableDictionaryYaml, yaml);
        }

        [Test]
        public void TestAbstractNonIdentifiableReferenceableDictionaryDeserialization()
        {
            var obj = (NonIdentifiableCollectionContainer)Deserialize(AbstractNonIdentifiableReferenceableDictionaryYaml);
            Assert.NotNull(obj.AbstractRefDictionary);
            Assert.AreEqual(2, obj.AbstractRefDictionary.Count);
            Assert.AreEqual(obj.AbstractRefDictionary["Item1"], obj.AbstractRefDictionary["Item2"]);
            Assert.AreEqual(GuidGenerator.Get(1), obj.AbstractRefDictionary["Item1"].Id);
            Assert.AreEqual("Test", obj.AbstractRefDictionary["Item1"].Value);
        }

        private static string SerializeAsString(object instance, YamlAssetMetadata<Guid> objectReferences)
        {
            using (var stream = new MemoryStream())
            {
                var metadata = new AttachedYamlAssetMetadata();
                if (objectReferences != null)
                {
                    metadata.AttachMetadata(AssetObjectSerializerBackend.ObjectReferencesKey, objectReferences);
                }

                new YamlAssetSerializer().Save(stream, instance, metadata);
                stream.Flush();
                stream.Position = 0;
                return new StreamReader(stream).ReadToEnd();
            }
        }

        private static object Deserialize(string yaml)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(yaml);
            writer.Flush();
            stream.Position = 0;
            bool aliasOccurred;
            AttachedYamlAssetMetadata metadata;
            var instance = new YamlAssetSerializer().Load(stream, "MyAsset", null, true, out aliasOccurred, out metadata);
            return instance;
        }
    }
}
