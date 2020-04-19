// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xunit;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Quantum.Tests
{
    public class TestReconcileWithBase
    {
        [Fact]
        public void TestPrimitiveMember()
        {
            const string primitiveMemberBaseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset1,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
MyString: MyBaseString
";
            const string primitiveMemberOverridenYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset1,Stride.Core.Assets.Quantum.Tests
Id: 30000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
MyString*: MyDerivedString
";
            const string primitiveMemberToReconcileYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset1,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
MyString: MyDerivedString
";
            var context = DeriveAssetTest<Types.MyAsset1, Types.MyAssetBasePropertyGraph>.LoadFromYaml(primitiveMemberBaseYaml, primitiveMemberOverridenYaml);
            Assert.Equal("MyBaseString", context.BaseAsset.MyString);
            Assert.Equal("MyDerivedString", context.DerivedAsset.MyString);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal("MyBaseString", context.BaseAsset.MyString);
            Assert.Equal("MyDerivedString", context.DerivedAsset.MyString);

            context = DeriveAssetTest<Types.MyAsset1, Types.MyAssetBasePropertyGraph>.LoadFromYaml(primitiveMemberBaseYaml, primitiveMemberToReconcileYaml);
            Assert.Equal("MyBaseString", context.BaseAsset.MyString);
            Assert.Equal("MyDerivedString", context.DerivedAsset.MyString);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal("MyBaseString", context.BaseAsset.MyString);
            Assert.Equal("MyBaseString", context.DerivedAsset.MyString);
        }

        [Fact]
        public void TestCollectionMismatchItem()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    14000000140000001400000014000000: String2
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000*: MyDerivedString
    14000000140000001400000014000000: MyBaseString
";
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            Assert.Equal(2, context.BaseAsset.MyStrings.Count);
            Assert.Equal("String1", context.BaseAsset.MyStrings[0]);
            Assert.Equal("String2", context.BaseAsset.MyStrings[1]);
            Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("MyDerivedString", context.DerivedAsset.MyStrings[0]);
            Assert.Equal("MyBaseString", context.DerivedAsset.MyStrings[1]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds[1]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds[1]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(2, context.BaseAsset.MyStrings.Count);
            Assert.Equal("String1", context.BaseAsset.MyStrings[0]);
            Assert.Equal("String2", context.BaseAsset.MyStrings[1]);
            Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("MyDerivedString", context.DerivedAsset.MyStrings[0]);
            Assert.Equal("String2", context.DerivedAsset.MyStrings[1]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds[1]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds[1]);
        }

        [Fact]
        public void TestCollectionMismatchId()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    14000000140000001400000014000000: String2
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    1a0000001a0000001a0000001a000000: String1
    14000000140000001400000014000000: String2
";
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            Assert.Equal(2, context.BaseAsset.MyStrings.Count);
            Assert.Equal("String1", context.BaseAsset.MyStrings[0]);
            Assert.Equal("String2", context.BaseAsset.MyStrings[1]);
            Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", context.DerivedAsset.MyStrings[0]);
            Assert.Equal("String2", context.DerivedAsset.MyStrings[1]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds[1]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(26), derivedIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds[1]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(2, context.BaseAsset.MyStrings.Count);
            Assert.Equal("String1", context.BaseAsset.MyStrings[0]);
            Assert.Equal("String2", context.BaseAsset.MyStrings[1]);
            Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", context.DerivedAsset.MyStrings[0]);
            Assert.Equal("String2", context.DerivedAsset.MyStrings[1]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds[1]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds[1]);
        }

        [Fact]
        public void TestCollectionAddedItemInBase()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    15000000150000001500000015000000: String2.5
    14000000140000001400000014000000: String2
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    14000000140000001400000014000000: String2
";
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            Assert.Equal(3, context.BaseAsset.MyStrings.Count);
            Assert.Equal("String1", context.BaseAsset.MyStrings[0]);
            Assert.Equal("String2.5", context.BaseAsset.MyStrings[1]);
            Assert.Equal("String2", context.BaseAsset.MyStrings[2]);
            Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", context.DerivedAsset.MyStrings[0]);
            Assert.Equal("String2", context.DerivedAsset.MyStrings[1]);
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds[0]);
            Assert.Equal(IdentifierGenerator.Get(21), baseIds[1]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds[2]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds[1]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(3, context.BaseAsset.MyStrings.Count);
            Assert.Equal("String1", context.BaseAsset.MyStrings[0]);
            Assert.Equal("String2.5", context.BaseAsset.MyStrings[1]);
            Assert.Equal("String2", context.BaseAsset.MyStrings[2]);
            Assert.Equal(3, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", context.DerivedAsset.MyStrings[0]);
            Assert.Equal("String2.5", context.DerivedAsset.MyStrings[1]);
            Assert.Equal("String2", context.DerivedAsset.MyStrings[2]);
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds[0]);
            Assert.Equal(IdentifierGenerator.Get(21), baseIds[1]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds[2]);
            Assert.Equal(3, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds[0]);
            Assert.Equal(IdentifierGenerator.Get(21), derivedIds[1]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds[2]);
        }

        [Fact]
        public void TestCollectionRemovedItemFromBase()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    14000000140000001400000014000000: String3
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    24000000240000002400000024000000: String2
    14000000140000001400000014000000: String3
";
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            Assert.Equal(2, context.BaseAsset.MyStrings.Count);
            Assert.Equal("String1", context.BaseAsset.MyStrings[0]);
            Assert.Equal("String3", context.BaseAsset.MyStrings[1]);
            Assert.Equal(3, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", context.DerivedAsset.MyStrings[0]);
            Assert.Equal("String2", context.DerivedAsset.MyStrings[1]);
            Assert.Equal("String3", context.DerivedAsset.MyStrings[2]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds[1]);
            Assert.Equal(3, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds[0]);
            Assert.Equal(IdentifierGenerator.Get(36), derivedIds[1]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds[2]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(2, context.BaseAsset.MyStrings.Count);
            Assert.Equal("String1", context.BaseAsset.MyStrings[0]);
            Assert.Equal("String3", context.BaseAsset.MyStrings[1]);
            Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", context.DerivedAsset.MyStrings[0]);
            Assert.Equal("String3", context.DerivedAsset.MyStrings[1]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds[1]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds[0]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds[1]);
        }

        [Fact]
        public void TestCollectionRemovedDeletedItemFromBase()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    24000000240000002400000024000000: String3
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset2,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
Struct:
    MyStrings: {}
MyStrings:
    0a0000000a0000000a0000000a000000: String1
    24000000240000002400000024000000: String2
    14000000140000001400000014000000: ~(Deleted)
";
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            Assert.Equal(2, context.BaseAsset.MyStrings.Count);
            Assert.Equal("String1", context.BaseAsset.MyStrings[0]);
            Assert.Equal("String3", context.BaseAsset.MyStrings[1]);
            Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", context.DerivedAsset.MyStrings[0]);
            Assert.Equal("String2", context.DerivedAsset.MyStrings[1]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds[0]);
            Assert.Equal(IdentifierGenerator.Get(36), baseIds[1]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(1, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds[0]);
            Assert.Equal(IdentifierGenerator.Get(36), derivedIds[1]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds.DeletedItems.Single());
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(2, context.BaseAsset.MyStrings.Count);
            Assert.Equal("String1", context.BaseAsset.MyStrings[0]);
            Assert.Equal("String3", context.BaseAsset.MyStrings[1]);
            Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", context.DerivedAsset.MyStrings[0]);
            Assert.Equal("String3", context.DerivedAsset.MyStrings[1]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds[0]);
            Assert.Equal(IdentifierGenerator.Get(36), baseIds[1]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds[0]);
            Assert.Equal(IdentifierGenerator.Get(36), derivedIds[1]);
        }

        [Fact]
        public void TestDictionaryMismatchValue()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    14000000140000001400000014000000~Key2: String2
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000*~Key1: MyDerivedString
    14000000140000001400000014000000~Key2: MyBaseString
";
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.BaseAsset.MyDictionary["Key2"]);
            Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("MyDerivedString", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("MyBaseString", context.DerivedAsset.MyDictionary["Key2"]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds["Key2"]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds["Key2"]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.BaseAsset.MyDictionary["Key2"]);
            Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("MyDerivedString", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.DerivedAsset.MyDictionary["Key2"]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds["Key2"]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds["Key2"]);
        }

        [Fact]
        public void TestDictionaryAddedKeyInBase()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    15000000150000001500000015000000~Key2.5: String2.5
    14000000140000001400000014000000~Key2: String2
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    14000000140000001400000014000000~Key2: String2
";
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            Assert.Equal(3, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String2.5", context.BaseAsset.MyDictionary["Key2.5"]);
            Assert.Equal("String2", context.BaseAsset.MyDictionary["Key2"]);
            Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.DerivedAsset.MyDictionary["Key2"]);
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(21), baseIds["Key2.5"]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds["Key2"]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds["Key2"]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(3, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String2.5", context.BaseAsset.MyDictionary["Key2.5"]);
            Assert.Equal("String2", context.BaseAsset.MyDictionary["Key2"]);
            Assert.Equal(3, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String2.5", context.DerivedAsset.MyDictionary["Key2.5"]);
            Assert.Equal("String2", context.DerivedAsset.MyDictionary["Key2"]);
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(21), baseIds["Key2.5"]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds["Key2"]);
            Assert.Equal(3, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(21), derivedIds["Key2.5"]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds["Key2"]);
        }

        [Fact]
        public void TestDictionaryKeyCollision()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    14000000140000001400000014000000~Key2: String2
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    15000000150000001500000015000000*~Key2: String3
";
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.BaseAsset.MyDictionary["Key2"]);
            Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String3", context.DerivedAsset.MyDictionary["Key2"]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds["Key2"]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(21), derivedIds["Key2"]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.BaseAsset.MyDictionary["Key2"]);
            Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String3", context.DerivedAsset.MyDictionary["Key2"]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds["Key2"]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(1, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(21), derivedIds["Key2"]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds.DeletedItems.Single());
        }

        [Fact]
        public void TestDictionaryRemovedItemFromBase()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    14000000140000001400000014000000~Key3: String3
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    24000000240000002400000024000000~Key2: String2
    14000000140000001400000014000000~Key3: String3
";
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String3", context.BaseAsset.MyDictionary["Key3"]);
            Assert.Equal(3, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.DerivedAsset.MyDictionary["Key2"]);
            Assert.Equal("String3", context.DerivedAsset.MyDictionary["Key3"]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds["Key3"]);
            Assert.Equal(3, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(36), derivedIds["Key2"]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds["Key3"]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String3", context.BaseAsset.MyDictionary["Key3"]);
            Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String3", context.DerivedAsset.MyDictionary["Key3"]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds["Key3"]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds["Key3"]);
        }

        [Fact]
        public void TestDictionaryRemovedDeletedItemFromBase()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    24000000240000002400000024000000~Key3: String3
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    24000000240000002400000024000000~Key3: String2
    14000000140000001400000014000000~: ~(Deleted)
";
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String3", context.BaseAsset.MyDictionary["Key3"]);
            Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.DerivedAsset.MyDictionary["Key3"]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(36), baseIds["Key3"]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(1, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(36), derivedIds["Key3"]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds.DeletedItems.Single());
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String3", context.BaseAsset.MyDictionary["Key3"]);
            Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String3", context.DerivedAsset.MyDictionary["Key3"]);
            Assert.Equal(2, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(36), baseIds["Key3"]);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(36), derivedIds["Key3"]);
        }

        [Fact]
        public void TestDictionaryRenameItemFromBase()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 10000000-0000-0000-0000-000000000000
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    24000000240000002400000024000000~Key2Renamed: String2
    14000000140000001400000014000000~Key3Renamed: String3
    34000000340000003400000034000000~Key4Renamed: String4
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAsset3,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 10000000-0000-0000-0000-000000000000:MyAsset
Tags: []
MyDictionary:
    0a0000000a0000000a0000000a000000~Key1: String1
    24000000240000002400000024000000~Key2: String2
    14000000140000001400000014000000*~Key3: MyDerivedString
    34000000340000003400000034000000~Key4*: MyDerivedString
";
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            Assert.Equal(4, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.BaseAsset.MyDictionary["Key2Renamed"]);
            Assert.Equal("String3", context.BaseAsset.MyDictionary["Key3Renamed"]);
            Assert.Equal("String4", context.BaseAsset.MyDictionary["Key4Renamed"]);
            Assert.Equal(4, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.DerivedAsset.MyDictionary["Key2"]);
            Assert.Equal("MyDerivedString", context.DerivedAsset.MyDictionary["Key3"]);
            Assert.Equal("MyDerivedString", context.DerivedAsset.MyDictionary["Key4"]);
            Assert.Equal(4, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(36), baseIds["Key2Renamed"]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds["Key3Renamed"]);
            Assert.Equal(IdentifierGenerator.Get(52), baseIds["Key4Renamed"]);
            Assert.Equal(4, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(36), derivedIds["Key2"]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds["Key3"]);
            Assert.Equal(IdentifierGenerator.Get(52), derivedIds["Key4"]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(4, context.BaseAsset.MyDictionary.Count);
            Assert.Equal("String1", context.BaseAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.BaseAsset.MyDictionary["Key2Renamed"]);
            Assert.Equal("String3", context.BaseAsset.MyDictionary["Key3Renamed"]);
            Assert.Equal(4, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", context.DerivedAsset.MyDictionary["Key1"]);
            Assert.Equal("String2", context.DerivedAsset.MyDictionary["Key2Renamed"]);
            Assert.Equal("MyDerivedString", context.DerivedAsset.MyDictionary["Key3Renamed"]);
            Assert.Equal("String4", context.DerivedAsset.MyDictionary["Key4"]);
            Assert.Equal(4, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), baseIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(36), baseIds["Key2Renamed"]);
            Assert.Equal(IdentifierGenerator.Get(20), baseIds["Key3Renamed"]);
            Assert.Equal(IdentifierGenerator.Get(52), baseIds["Key4Renamed"]);
            Assert.Equal(4, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(IdentifierGenerator.Get(10), derivedIds["Key1"]);
            Assert.Equal(IdentifierGenerator.Get(36), derivedIds["Key2Renamed"]);
            Assert.Equal(IdentifierGenerator.Get(20), derivedIds["Key3Renamed"]);
            Assert.Equal(IdentifierGenerator.Get(52), derivedIds["Key4"]);
        }
    }
}
