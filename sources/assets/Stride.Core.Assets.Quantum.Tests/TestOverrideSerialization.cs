// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using System.Linq;
using Xunit;
using Xenko.Core.Assets.Quantum.Internal;
using Xenko.Core.Assets.Quantum.Tests.Helpers;
using Xenko.Core.Assets.Tests.Helpers;
using Xenko.Core.Assets.Yaml;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Quantum.Tests
{
    public class TestOverrideSerialization
    {
        /* test TODO:
         * Non-abstract class (test result recursively) : simple prop + in collection
         * Abstract (interface) override with different type
         * class prop set to null
         */

        private const string SimplePropertyUpdateBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset1,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyString: MyBaseString
";
        private const string SimplePropertyUpdateDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset1,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyString*: MyDerivedString
";
        private const string SimplePropertyWithOverrideToDefaultValueBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset10,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyBool: false
";
        private const string SimplePropertyWithOverrideToDefaultValueDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset10,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyBool*: true
";
        private const string SimpleCollectionUpdateBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    14000000140000001400000014000000: MyBaseString
";
        private const string SimpleCollectionUpdateDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000*: MyDerivedString
    14000000140000001400000014000000: MyBaseString
";
        private const string SimpleDictionaryUpdateBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    14000000140000001400000014000000~Key2: MyBaseString
";
        private const string SimpleDictionaryUpdateDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyDictionary:
    0a0000000a0000000a0000000a000000*~Key1: MyDerivedString
    14000000140000001400000014000000~Key2: MyBaseString
";
        private const string CollectionInStructBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
Struct:
    MyStrings:
        0a0000000a0000000a0000000a000000: String1
        14000000140000001400000014000000: MyBaseString
MyStrings: {}
";
        private const string CollectionInStructDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
Struct:
    MyStrings:
        0a0000000a0000000a0000000a000000*: MyDerivedString
        14000000140000001400000014000000: MyBaseString
MyStrings: {}
";
        private const string SimpleCollectionAddBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    14000000140000001400000014000000: String2
    {0}: String4
";
        private const string SimpleCollectionAddDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    14000000140000001400000014000000: String2
    {0}: String4
    {1}*: String3
";
        private const string SimpleDictionaryAddBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    14000000140000001400000014000000~Key2: String2
    {0}~Key4: String4
";
        private const string SimpleDictionaryAddDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    14000000140000001400000014000000~Key2: String2
    {1}*~Key3: String3
    {0}~Key4: String4
";
        private const string ObjectCollectionUpdateBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset4,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value: String1
    14000000140000001400000014000000:
        Value: MyBaseString
";
        private const string ObjectCollectionUpdateDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset4,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObjects:
    0a0000000a0000000a0000000a000000*:
        Value: MyDerivedString
    14000000140000001400000014000000:
        Value: MyBaseString
";
        private const string ObjectCollectionAddBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset4,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value: String1
    14000000140000001400000014000000:
        Value: String2
    {0}:
        Value: String4
";
        private const string ObjectCollectionAddDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset4,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value: String1
    14000000140000001400000014000000:
        Value: String2
    {0}:
        Value: String4
    {1}*:
        Value: String3
";
        private const string ObjectCollectionPropertyUpdateBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset4,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value: String1
    14000000140000001400000014000000:
        Value: MyBaseString
";
        private const string ObjectCollectionPropertyUpdateDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset4,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value*: MyDerivedString
    14000000140000001400000014000000:
        Value: MyBaseString
";
        private const string NonIdentifiableObjectCollectionPropertyUpdateBaseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset8,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyObjects:
    -   Value: String1
    -   Value: MyBaseString
";
        private const string NonIdentifiableObjectCollectionPropertyUpdateDerivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset8,Xenko.Core.Assets.Quantum.Tests
Id: 00000002-0002-0000-0200-000002000000
Tags: []
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObjects:
    -   Value*: MyDerivedString
    -   Value: MyBaseString
";

        [Fact]
        public void TestSimplePropertySerialization()
        {
            var asset = new Types.MyAsset1 { MyString = "String" };
            var context = DeriveAssetTest<Types.MyAsset1, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset1.MyString)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset1.MyString)];

            basePropertyNode.Update("MyBaseString");
            derivedPropertyNode.Update("MyDerivedString");
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, SimplePropertyUpdateBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, SimplePropertyUpdateDerivedYaml, true);
        }

        [Fact]
        public void TestSimplePropertyDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset1, Types.MyAssetBasePropertyGraph>.LoadFromYaml(SimplePropertyUpdateBaseYaml, SimplePropertyUpdateDerivedYaml);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset1.MyString)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset1.MyString)];

            Assert.Equal("MyBaseString", basePropertyNode.Retrieve());
            Assert.Equal("MyDerivedString", derivedPropertyNode.Retrieve());
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.New, derivedPropertyNode.GetContentOverride());
        }

        [Fact]
        public void TestSimplePropertyWithOverrideToDefaultValueSerialization()
        {
            var asset = new Types.MyAsset10 { MyBool = false };
            var context = DeriveAssetTest<Types.MyAsset10, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var derivedPropertyNode = (AssetMemberNode)context.DerivedGraph.RootNode[nameof(Types.MyAsset10.MyBool)];

            derivedPropertyNode.Update(true);
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, SimplePropertyWithOverrideToDefaultValueBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, SimplePropertyWithOverrideToDefaultValueDerivedYaml, true);
        }

        [Fact]
        public void TestSimplePropertyWithOverrideToDefaultValueDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset10, Types.MyAssetBasePropertyGraph>.LoadFromYaml(SimplePropertyWithOverrideToDefaultValueBaseYaml, SimplePropertyWithOverrideToDefaultValueDerivedYaml);
            var basePropertyNode = (AssetMemberNode)context.BaseGraph.RootNode[nameof(Types.MyAsset10.MyBool)];
            var derivedPropertyNode = (AssetMemberNode)context.DerivedGraph.RootNode[nameof(Types.MyAsset10.MyBool)];

            Assert.Equal(false, basePropertyNode.Retrieve());
            Assert.Equal(true, derivedPropertyNode.Retrieve());
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.New, derivedPropertyNode.GetContentOverride());
        }

        [Fact]
        public void TestSimpleCollectionUpdateSerialization()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2" } };
            var ids = CollectionItemIdHelper.GetCollectionItemIds(asset.MyStrings);
            ids.Add(0, IdentifierGenerator.Get(10));
            ids.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];

            basePropertyNode.Target.Update("MyBaseString", new NodeIndex(1));
            derivedPropertyNode.Target.Update("MyDerivedString", new NodeIndex(0));
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, SimpleCollectionUpdateBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, SimpleCollectionUpdateDerivedYaml, true);
        }

        [Fact]
        public void TestSimpleCollectionUpdateDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.LoadFromYaml(SimpleCollectionUpdateBaseYaml, SimpleCollectionUpdateDerivedYaml);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);

            Assert.Equal(2, context.BaseAsset.MyStrings.Count);
            Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("MyBaseString", basePropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("MyDerivedString", derivedPropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("MyBaseString", derivedPropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.NotSame(baseIds, derivedIds);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[1], derivedIds[1]);
        }

        [Fact]
        public void TestSimpleDictionaryUpdateSerialization()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" } } };
            var ids = CollectionItemIdHelper.GetCollectionItemIds(asset.MyDictionary);
            ids.Add("Key1", IdentifierGenerator.Get(10));
            ids.Add("Key2", IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];

            basePropertyNode.Target.Update("MyBaseString", new NodeIndex("Key2"));
            derivedPropertyNode.Target.Update("MyDerivedString", new NodeIndex("Key1"));
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, SimpleDictionaryUpdateBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, SimpleDictionaryUpdateDerivedYaml, true);

            context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.LoadFromYaml(SimpleDictionaryUpdateBaseYaml, SimpleDictionaryUpdateDerivedYaml);
            basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);

            Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
            Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("MyBaseString", basePropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal("MyDerivedString", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("MyBaseString", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.NotSame(baseIds, derivedIds);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
            Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
        }

        [Fact]
        public void TestSimpleDictionaryDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.LoadFromYaml(SimpleDictionaryUpdateBaseYaml, SimpleDictionaryUpdateDerivedYaml);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);

            Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
            Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("MyBaseString", basePropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal("MyDerivedString", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("MyBaseString", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.NotSame(baseIds, derivedIds);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
            Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
        }

        [Fact]
        public void TestCollectionInStructUpdateSerialization()
        {
            var asset = new Types.MyAsset2();
            asset.Struct.MyStrings.Add("String1");
            asset.Struct.MyStrings.Add("String2");
            var ids = CollectionItemIdHelper.GetCollectionItemIds(asset.Struct.MyStrings);
            ids.Add(0, IdentifierGenerator.Get(10));
            ids.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.Struct)].Target[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.Struct)].Target[nameof(Types.MyAsset2.MyStrings)];

            basePropertyNode.Target.Update("MyBaseString", new NodeIndex(1));
            derivedPropertyNode.Target.Update("MyDerivedString", new NodeIndex(0));
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, CollectionInStructBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, CollectionInStructDerivedYaml, true);
        }

        [Fact]
        public void TestCollectionInStructUpdateDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.LoadFromYaml(CollectionInStructBaseYaml, CollectionInStructDerivedYaml);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.Struct)].Target[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.Struct)].Target[nameof(Types.MyAsset2.MyStrings)];
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.Struct.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.Struct.MyStrings);

            Assert.Equal(2, context.BaseAsset.Struct.MyStrings.Count);
            Assert.Equal(2, context.DerivedAsset.Struct.MyStrings.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("MyBaseString", basePropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("MyDerivedString", derivedPropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("MyBaseString", derivedPropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.NotSame(baseIds, derivedIds);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[1], derivedIds[1]);
        }

        [Fact]
        public void TestSimpleCollectionAddSerialization()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2" } };
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyStrings);
            baseIds.Add(0, IdentifierGenerator.Get(10));
            baseIds.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];

            derivedPropertyNode.Target.Add("String3");
            basePropertyNode.Target.Add("String4");
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var expectedBaseYaml = string.Format(SimpleCollectionAddBaseYaml.Replace("{}", "{{}}"), baseIds[2]);
            var expectedDerivedYaml = string.Format(SimpleCollectionAddDerivedYaml.Replace("{}", "{{}}"), baseIds[2], derivedIds[3]);
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, expectedBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, expectedDerivedYaml, true);
        }

        [Fact]
        public void TestSimpleCollectionAddDeserialization()
        {
            var expectedBaseYaml = string.Format(SimpleCollectionAddBaseYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30));
            var expectedDerivedYaml = string.Format(SimpleCollectionAddDerivedYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30), IdentifierGenerator.Get(40));
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.LoadFromYaml(expectedBaseYaml, expectedDerivedYaml);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);

            Assert.Equal(3, context.BaseAsset.MyStrings.Count);
            Assert.Equal(4, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String4", basePropertyNode.Retrieve(new NodeIndex(2)));
            Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String4", derivedPropertyNode.Retrieve(new NodeIndex(2)));
            Assert.Equal("String3", derivedPropertyNode.Retrieve(new NodeIndex(3)));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(3)));
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(4, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[1], derivedIds[1]);
            Assert.Equal(baseIds[2], derivedIds[2]);
        }

        [Fact]
        public void TestSimpleDictionaryAddSerialization()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" } } };
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyDictionary);
            baseIds.Add("Key1", IdentifierGenerator.Get(10));
            baseIds.Add("Key2", IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];

            // Update derived and check
            derivedPropertyNode.Target.Add("String3", new NodeIndex("Key3"));
            basePropertyNode.Target.Add("String4", new NodeIndex("Key4"));

            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var expectedBaseYaml = string.Format(SimpleDictionaryAddBaseYaml.Replace("{}", "{{}}"), baseIds["Key4"]);
            var expectedDerivedYaml = string.Format(SimpleDictionaryAddDerivedYaml.Replace("{}", "{{}}"), baseIds["Key4"], derivedIds["Key3"]);
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, expectedBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, expectedDerivedYaml, true);
        }

        [Fact]
        public void TestSimpleDictionaryAddDeserialization()
        {
            var expectedBaseYaml = string.Format(SimpleDictionaryAddBaseYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30));
            var expectedDerivedYaml = string.Format(SimpleDictionaryAddDerivedYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30), IdentifierGenerator.Get(40));
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.LoadFromYaml(expectedBaseYaml, expectedDerivedYaml);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);

            Assert.Equal(3, context.BaseAsset.MyDictionary.Count);
            Assert.Equal(4, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal("String4", basePropertyNode.Retrieve(new NodeIndex("Key4")));
            Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal("String3", derivedPropertyNode.Retrieve(new NodeIndex("Key3")));
            Assert.Equal("String4", derivedPropertyNode.Retrieve(new NodeIndex("Key4")));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key4")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key3")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key4")));
            Assert.NotSame(baseIds, derivedIds);
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(4, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
            Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            Assert.Equal(baseIds["Key4"], derivedIds["Key4"]);
            Assert.Equal(3, context.BaseAsset.MyDictionary.Count);
            Assert.Equal(4, context.DerivedAsset.MyDictionary.Count);
        }

        [Fact]
        public void TestObjectCollectionUpdateSerialization()
        {
            var asset = new Types.MyAsset4 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyObjects);
            baseIds.Add(0, IdentifierGenerator.Get(10));
            baseIds.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset4, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];

            basePropertyNode.Target.Update(new Types.SomeObject { Value = "MyBaseString" }, new NodeIndex(1));
            derivedPropertyNode.Target.Update(new Types.SomeObject { Value = "MyDerivedString" }, new NodeIndex(0));
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, ObjectCollectionUpdateBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, ObjectCollectionUpdateDerivedYaml, true);
        }

        [Fact]
        public void TestObjectCollectionUpdateDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset4, Types.MyAssetBasePropertyGraph>.LoadFromYaml(ObjectCollectionUpdateBaseYaml, ObjectCollectionUpdateDerivedYaml);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyObjects);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyObjects);

            Assert.Equal(2, context.BaseAsset.MyObjects.Count);
            Assert.Equal(2, context.DerivedAsset.MyObjects.Count);
            Assert.Equal("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
            Assert.Equal("MyBaseString", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
            Assert.Equal("MyDerivedString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
            Assert.Equal("MyBaseString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.NotSame(baseIds, derivedIds);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[1], derivedIds[1]);
        }

        [Fact]
        public void TestObjectCollectionAddSerialization()
        {
            var asset = new Types.MyAsset4 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyObjects);
            baseIds.Add(0, IdentifierGenerator.Get(10));
            baseIds.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset4, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyObjects);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];

            derivedPropertyNode.Target.Add(new Types.SomeObject { Value = "String3" });
            basePropertyNode.Target.Add(new Types.SomeObject { Value = "String4" });
            var expectedBaseYaml = string.Format(ObjectCollectionAddBaseYaml.Replace("{}", "{{}}"), baseIds[2]);
            var expectedDerivedYaml = string.Format(ObjectCollectionAddDerivedYaml.Replace("{}", "{{}}"), baseIds[2], derivedIds[3]);
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, expectedBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, expectedDerivedYaml, true);
        }

        [Fact]
        public void TestObjectCollectionAddDeserialization()
        {
            var expectedBaseYaml = string.Format(ObjectCollectionAddBaseYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30));
            var expectedDerivedYaml = string.Format(ObjectCollectionAddDerivedYaml.Replace("{}", "{{}}"), IdentifierGenerator.Get(30), IdentifierGenerator.Get(40));
            var context = DeriveAssetTest<Types.MyAsset4, Types.MyAssetBasePropertyGraph>.LoadFromYaml(expectedBaseYaml, expectedDerivedYaml);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyObjects);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyObjects);

            Assert.Equal(3, context.BaseAsset.MyObjects.Count);
            Assert.Equal(4, context.DerivedAsset.MyObjects.Count);
            Assert.Equal("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
            Assert.Equal("String2", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
            Assert.Equal("String4", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(2))).Value);
            Assert.Equal("String1", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
            Assert.Equal("String2", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
            Assert.Equal("String4", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(2))).Value);
            Assert.Equal("String3", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(3))).Value);
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(2)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(3)));
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(2)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(3)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.NotSame(baseIds, derivedIds);
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(4, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[1], derivedIds[1]);
            Assert.Equal(baseIds[2], derivedIds[2]);
        }

        [Fact]
        public void TestObjectCollectionPropertyUpdateSerialization()
        {
            var asset = new Types.MyAsset4 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyObjects);
            baseIds.Add(0, IdentifierGenerator.Get(10));
            baseIds.Add(1, IdentifierGenerator.Get(20));
            var context = DeriveAssetTest<Types.MyAsset4, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];

            basePropertyNode.Target.IndexedTarget(new NodeIndex(1))[nameof(Types.SomeObject.Value)].Update("MyBaseString");
            derivedPropertyNode.Target.IndexedTarget(new NodeIndex(0))[nameof(Types.SomeObject.Value)].Update("MyDerivedString");
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, ObjectCollectionPropertyUpdateBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, ObjectCollectionPropertyUpdateDerivedYaml, true);
        }

        [Fact]
        public void TestObjectCollectionPropertyUpdateDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset4, Types.MyAssetBasePropertyGraph>.LoadFromYaml(ObjectCollectionPropertyUpdateBaseYaml, ObjectCollectionPropertyUpdateDerivedYaml);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyObjects);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyObjects);

            Assert.Equal(2, context.BaseAsset.MyObjects.Count);
            Assert.Equal(2, context.DerivedAsset.MyObjects.Count);
            Assert.Equal("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
            Assert.Equal("MyBaseString", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
            Assert.Equal("MyDerivedString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
            Assert.Equal("MyBaseString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.New, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.NotSame(baseIds, derivedIds);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[1], derivedIds[1]);
        }

        [Fact]
        public void TestNonIdentifiableObjectCollectionUpdateSerialization()
        {
            var asset = new Types.MyAsset8 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset8, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset8.MyObjects)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset8.MyObjects)];
            // Manually link base of non-identifiable items - this simulates a scenario similar to prefabs
            context.DerivedGraph.RegisterCustomBaseLink(derivedPropertyNode.Target.IndexedTarget(new NodeIndex(0)), basePropertyNode.Target.IndexedTarget(new NodeIndex(0)));
            context.DerivedGraph.RegisterCustomBaseLink(derivedPropertyNode.Target.IndexedTarget(new NodeIndex(1)), basePropertyNode.Target.IndexedTarget(new NodeIndex(1)));
            context.DerivedGraph.RefreshBase();

            basePropertyNode.Target.IndexedTarget(new NodeIndex(1))[nameof(Types.SomeObject.Value)].Update("MyBaseString");
            derivedPropertyNode.Target.IndexedTarget(new NodeIndex(0))[nameof(Types.SomeObject.Value)].Update("MyDerivedString");
            SerializationHelper.SerializeAndCompare(context.BaseAssetItem, context.BaseGraph, NonIdentifiableObjectCollectionPropertyUpdateBaseYaml, false);
            SerializationHelper.SerializeAndCompare(context.DerivedAssetItem, context.DerivedGraph, NonIdentifiableObjectCollectionPropertyUpdateDerivedYaml, true);
        }

        [Fact]
        public void TestNonIdentifiableObjectCollectionUpdateDeserialization()
        {
            var context = DeriveAssetTest<Types.MyAsset8, Types.MyAssetBasePropertyGraph>.LoadFromYaml(NonIdentifiableObjectCollectionPropertyUpdateBaseYaml, NonIdentifiableObjectCollectionPropertyUpdateDerivedYaml);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset8.MyObjects)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset8.MyObjects)];
            // Manually link base of non-identifiable items - this simulates a scenario similar to prefabs
            context.DerivedGraph.RegisterCustomBaseLink(derivedPropertyNode.Target.IndexedTarget(new NodeIndex(0)), basePropertyNode.Target.IndexedTarget(new NodeIndex(0)));
            context.DerivedGraph.RegisterCustomBaseLink(derivedPropertyNode.Target.IndexedTarget(new NodeIndex(1)), basePropertyNode.Target.IndexedTarget(new NodeIndex(1)));
            context.DerivedGraph.RefreshBase();

            Assert.Equal(2, context.BaseAsset.MyObjects.Count);
            Assert.Equal(2, context.DerivedAsset.MyObjects.Count);
            Assert.Equal("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
            Assert.Equal("MyBaseString", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
            Assert.Equal("MyDerivedString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
            Assert.Equal("MyBaseString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.New, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
            Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
        }

        [Fact]
        public void TestGenerateOverridesForSerializationOfObjectMember()
        {
            const string expectedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+SomeObject,Xenko.Core.Assets.Quantum.Tests
Value*: OverriddenString
";
            var asset = new Types.MyAsset9 { MyObject = new Types.SomeObject { Value = "String1" } };
            var context = DeriveAssetTest<Types.MyAsset9, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset9.MyObject)];
            derivedPropertyNode.Target[nameof(Types.SomeObject.Value)].Update("OverriddenString");
            var expectedPath = new YamlAssetPath();
            expectedPath.PushMember(nameof(Types.SomeObject.Value));

            var overrides = AssetPropertyGraph.GenerateOverridesForSerialization(derivedPropertyNode);
            var overridesAsDictionary = overrides.ToDictionary(x => x.Key, x => x.Value, YamlAssetPathComparer.Default);
            Assert.Single(overridesAsDictionary);
            Assert.True(overridesAsDictionary.ContainsKey(expectedPath));
            Assert.Equal(OverrideType.New, overridesAsDictionary[expectedPath]);

            // We expect the same resulting path both from the member node and the target object node
            overrides = AssetPropertyGraph.GenerateOverridesForSerialization(derivedPropertyNode.Target);
            overridesAsDictionary = overrides.ToDictionary(x => x.Key, x => x.Value, YamlAssetPathComparer.Default);
            Assert.Single(overridesAsDictionary);
            Assert.True(overridesAsDictionary.ContainsKey(expectedPath));
            Assert.Equal(OverrideType.New, overridesAsDictionary[expectedPath]);

            // Test deserialization
            SerializationHelper.SerializeAndCompare(context.DerivedAsset.MyObject, overrides, expectedYaml);
            bool aliasOccurred;
            AttachedYamlAssetMetadata metadata;
            var instance = (Types.SomeObject)AssetFileSerializer.Default.Load(AssetTestContainer.ToStream(expectedYaml), null, null, true, out aliasOccurred, out metadata);
            overrides = metadata.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey);
            Assert.NotNull(overrides);
            overridesAsDictionary = overrides.ToDictionary(x => x.Key, x => x.Value, YamlAssetPathComparer.Default);
            Assert.Equal("OverriddenString", instance.Value);
            Assert.Single(overridesAsDictionary);
            Assert.True(overridesAsDictionary.ContainsKey(expectedPath));
            Assert.Equal(OverrideType.New, overridesAsDictionary[expectedPath]);
        }

        [Fact]
        public void TestGenerateOverridesForSerializationOfCollectionItem()
        {
            const string expectedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+SomeObject,Xenko.Core.Assets.Quantum.Tests
Value*: OverriddenString
";
            var asset = new Types.MyAsset4 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset4, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var derivedPropertyNode = (AssetObjectNode)context.DerivedGraph.RootNode[nameof(Types.MyAsset4.MyObjects)].Target.IndexedTarget(new NodeIndex(1));
            derivedPropertyNode[nameof(Types.SomeObject.Value)].Update("OverriddenString");
            var expectedPath = new YamlAssetPath();
            expectedPath.PushMember(nameof(Types.SomeObject.Value));

            var overrides = AssetPropertyGraph.GenerateOverridesForSerialization(derivedPropertyNode);
            var overridesAsDictionary = overrides.ToDictionary(x => x.Key, x => x.Value, YamlAssetPathComparer.Default);
            Assert.Single(overridesAsDictionary);
            Assert.True(overridesAsDictionary.ContainsKey(expectedPath));
            Assert.Equal(OverrideType.New, overridesAsDictionary[expectedPath]);

            // Test deserialization
            SerializationHelper.SerializeAndCompare(context.DerivedAsset.MyObjects[1], overrides, expectedYaml);
            bool aliasOccurred;
            AttachedYamlAssetMetadata metadata;
            var instance = (Types.SomeObject)AssetFileSerializer.Default.Load(AssetTestContainer.ToStream(expectedYaml), null, null, true, out aliasOccurred, out metadata);
            overrides = metadata.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey);
            Assert.NotNull(overrides);
            overridesAsDictionary = overrides.ToDictionary(x => x.Key, x => x.Value, YamlAssetPathComparer.Default);
            Assert.Equal("OverriddenString", instance.Value);
            Assert.Single(overridesAsDictionary);
            Assert.True(overridesAsDictionary.ContainsKey(expectedPath));
            Assert.Equal(OverrideType.New, overridesAsDictionary[expectedPath]);
        }
    }
}

