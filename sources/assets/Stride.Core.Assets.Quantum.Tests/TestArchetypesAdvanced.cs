// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xunit;
using Xenko.Core.Assets.Quantum.Tests.Helpers;
using Xenko.Core.Reflection;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Quantum.Tests
{
    public class TestArchetypesAdvanced
    {
        [Fact]
        public void TestSimpleDictionaryAddWithCollision()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];

            // Update a key to derived and then the same key to the base
            derivedPropertyNode.Target.Add("String3", new NodeIndex("Key3"));
            basePropertyNode.Target.Add("String4", new NodeIndex("Key3"));

            Assert.Equal(3, context.BaseAsset.MyDictionary.Count);
            Assert.Equal(3, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal("String4", basePropertyNode.Retrieve(new NodeIndex("Key3")));
            Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal("String3", derivedPropertyNode.Retrieve(new NodeIndex("Key3")));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key3")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key3")));
            Assert.NotEqual(baseIds, derivedIds);
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(3, derivedIds.KeyCount);
            Assert.Equal(1, derivedIds.DeletedCount);
            Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
            Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            Assert.NotEqual(baseIds["Key3"], derivedIds["Key3"]);
            Assert.Equal(baseIds["Key3"], derivedIds.DeletedItems.Single());
            Assert.Equal(3, context.BaseAsset.MyDictionary.Count);
            Assert.Equal(3, context.DerivedAsset.MyDictionary.Count);
        }

        [Fact]
        public void TestSimpleCollectionRemoveDeleted()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2", "String3", "String4" } };
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];

            // Delete an item from the derived and then delete the same from the base
            var derivedDeletedId = derivedIds[2];
            var baseDeletedId = baseIds[2];
            derivedPropertyNode.Target.Remove("String3", new NodeIndex(2));
            basePropertyNode.Target.Remove("String3", new NodeIndex(2));
            Assert.Equal(3, context.BaseAsset.MyStrings.Count);
            Assert.Equal(3, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String4", basePropertyNode.Retrieve(new NodeIndex(2)));
            Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String4", derivedPropertyNode.Retrieve(new NodeIndex(2)));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(3, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[1], derivedIds[1]);
            Assert.Equal(baseIds[2], derivedIds[2]);
            Assert.False(derivedIds.IsDeleted(derivedDeletedId));
            Assert.False(baseIds.IsDeleted(baseDeletedId));
        }

        [Fact]
        public void TestSimpleDictionaryRemoveDeleted()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" }, { "Key3", "String3" }, { "Key4", "String4" } } };
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];

            // Delete an item from the derived and then delete the same from the base
            var derivedDeletedId = derivedIds["Key3"];
            derivedPropertyNode.Target.Remove("String3", new NodeIndex("Key3"));
            var baseDeletedId = baseIds["Key3"];
            basePropertyNode.Target.Remove("String3", new NodeIndex("Key3"));
            Assert.Equal(3, context.BaseAsset.MyDictionary.Count);
            Assert.Equal(3, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal("String4", basePropertyNode.Retrieve(new NodeIndex("Key4")));
            Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal("String4", derivedPropertyNode.Retrieve(new NodeIndex("Key4")));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key4")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key4")));
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(3, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
            Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            Assert.Equal(baseIds["Key4"], derivedIds["Key4"]);
            Assert.False(derivedIds.IsDeleted(derivedDeletedId));
            Assert.False(baseIds.IsDeleted(baseDeletedId));
        }

        [Fact]
        public void TestSimpleCollectionUpdateDeleted()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2", "String3", "String4" } };
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];

            // Delete an item from the derived and then update the same from the base
            var derivedDeletedId = derivedIds[2];
            derivedPropertyNode.Target.Remove("String3", new NodeIndex(2));
            basePropertyNode.Target.Update("String3.5", new NodeIndex(2));
            Assert.Equal(4, context.BaseAsset.MyStrings.Count);
            Assert.Equal(3, context.DerivedAsset.MyStrings.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String3.5", basePropertyNode.Retrieve(new NodeIndex(2)));
            Assert.Equal("String4", basePropertyNode.Retrieve(new NodeIndex(3)));
            Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String4", derivedPropertyNode.Retrieve(new NodeIndex(2)));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(3)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(4, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(3, derivedIds.KeyCount);
            Assert.Equal(1, derivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[1], derivedIds[1]);
            Assert.Equal(baseIds[3], derivedIds[2]);
            Assert.True(derivedIds.IsDeleted(derivedDeletedId));
        }

        [Fact]
        public void TestSimpleDictionaryUpdateDeleted()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" }, { "Key3", "String3" }, { "Key4", "String4" } } };
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];

            // Delete an item from the derived and then update the same from the base
            var derivedDeletedId = derivedIds["Key3"];
            derivedPropertyNode.Target.Remove("String3", new NodeIndex("Key3"));
            basePropertyNode.Target.Update("String3.5", new NodeIndex("Key3"));
            Assert.Equal(4, context.BaseAsset.MyDictionary.Count);
            Assert.Equal(3, context.DerivedAsset.MyDictionary.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal("String3.5", basePropertyNode.Retrieve(new NodeIndex("Key3")));
            Assert.Equal("String4", basePropertyNode.Retrieve(new NodeIndex("Key4")));
            Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
            Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
            Assert.Equal("String4", derivedPropertyNode.Retrieve(new NodeIndex("Key4")));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key3")));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key4")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key4")));
            Assert.Equal(4, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(3, derivedIds.KeyCount);
            Assert.Equal(1, derivedIds.DeletedCount);
            Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
            Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            Assert.Equal(baseIds["Key4"], derivedIds["Key4"]);
            Assert.True(derivedIds.IsDeleted(derivedDeletedId));
        }

        [Fact]
        public void TestSimpleCollectionAddMultipleAndCheckOrder()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2", "String3", "String4" } };
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];

            derivedPropertyNode.Target.Add("String3.5", new NodeIndex(3));
            derivedPropertyNode.Target.Add("String1.5", new NodeIndex(1));
            Assert.Equal(6, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String1", "String1.5", "String2", "String3", "String3.5", "String4");

            basePropertyNode.Target.Add("String0.1", new NodeIndex(0));
            Assert.Equal(5, context.BaseAsset.MyStrings.Count);
            AssertCollection(basePropertyNode, "String0.1", "String1", "String2", "String3", "String4");
            Assert.Equal(7, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String0.1", "String1", "String1.5", "String2", "String3", "String3.5", "String4");

            basePropertyNode.Target.Add("String1.1", new NodeIndex(2));
            Assert.Equal(6, context.BaseAsset.MyStrings.Count);
            AssertCollection(basePropertyNode, "String0.1", "String1", "String1.1", "String2", "String3", "String4");
            Assert.Equal(8, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String0.1", "String1", "String1.1", "String1.5", "String2", "String3", "String3.5", "String4");

            basePropertyNode.Target.Add("String2.1", new NodeIndex(4));
            Assert.Equal(7, context.BaseAsset.MyStrings.Count);
            AssertCollection(basePropertyNode, "String0.1", "String1", "String1.1", "String2", "String2.1", "String3", "String4");
            Assert.Equal(9, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String0.1", "String1", "String1.1", "String1.5", "String2", "String2.1", "String3", "String3.5", "String4");

            basePropertyNode.Target.Add("String3.1", new NodeIndex(6));
            Assert.Equal(8, context.BaseAsset.MyStrings.Count);
            AssertCollection(basePropertyNode, "String0.1", "String1", "String1.1", "String2", "String2.1", "String3", "String3.1", "String4");
            Assert.Equal(10, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String0.1", "String1", "String1.1", "String1.5", "String2", "String2.1", "String3", "String3.1", "String3.5", "String4");

            basePropertyNode.Target.Add("String4.1", new NodeIndex(8));
            Assert.Equal(9, context.BaseAsset.MyStrings.Count);
            AssertCollection(basePropertyNode, "String0.1", "String1", "String1.1", "String2", "String2.1", "String3", "String3.1", "String4", "String4.1");
            Assert.Equal(11, context.DerivedAsset.MyStrings.Count);
            AssertCollection(derivedPropertyNode, "String0.1", "String1", "String1.1", "String1.5", "String2", "String2.1", "String3", "String3.1", "String3.5", "String4", "String4.1");

            Assert.Equal(9, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(11, derivedIds.KeyCount);
            Assert.Equal(0, derivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[1], derivedIds[1]);
            Assert.Equal(baseIds[2], derivedIds[2]);
            Assert.Equal(baseIds[3], derivedIds[4]);
            Assert.Equal(baseIds[4], derivedIds[5]);
            Assert.Equal(baseIds[5], derivedIds[6]);
            Assert.Equal(baseIds[6], derivedIds[7]);
            Assert.Equal(baseIds[7], derivedIds[9]);
            Assert.Equal(baseIds[8], derivedIds[10]);
        }

        [Fact]
        public void TestRemoveBaseAddDerivedWithSubDerived()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2" } };
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var subDerivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.SubDerivedAsset.MyStrings);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var subDerivedPropertyNode = context.SubDerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];

            // Delete an item from the derived and then update the same from the base
            var derivedDeletedId = derivedIds[1];
            derivedPropertyNode.Target.Remove("String2", new NodeIndex(1));
            basePropertyNode.Target.Add("String3");
            Assert.Equal(3, context.BaseAsset.MyStrings.Count);
            Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
            Assert.Equal(2, context.SubDerivedAsset.MyStrings.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String3", basePropertyNode.Retrieve(new NodeIndex(2)));
            Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String3", derivedPropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String1", subDerivedPropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String3", subDerivedPropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, subDerivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, subDerivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, subDerivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(3, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(2, derivedIds.KeyCount);
            Assert.Equal(1, derivedIds.DeletedCount);
            Assert.Equal(2, subDerivedIds.KeyCount);
            Assert.Equal(0, subDerivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[2], derivedIds[1]);
            Assert.Equal(derivedIds[0], subDerivedIds[0]);
            Assert.Equal(derivedIds[1], subDerivedIds[1]);
            Assert.True(derivedIds.IsDeleted(derivedDeletedId));
        }

        [Fact]
        public void TestAddBaseRemoveDerivedAndAddInBaseWithSubDerived()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2" } };
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var subDerivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.SubDerivedAsset.MyStrings);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var subDerivedPropertyNode = context.SubDerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];

            // Delete an item from the derived and then update the same from the base
            basePropertyNode.Target.Add("String3");
            var derivedDeletedId = derivedIds[2];
            derivedPropertyNode.Target.Remove("String3", new NodeIndex(2));
            basePropertyNode.Target.Add("String4");
            Assert.Equal(4, context.BaseAsset.MyStrings.Count);
            Assert.Equal(3, context.DerivedAsset.MyStrings.Count);
            Assert.Equal(3, context.SubDerivedAsset.MyStrings.Count);
            Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String3", basePropertyNode.Retrieve(new NodeIndex(2)));
            Assert.Equal("String4", basePropertyNode.Retrieve(new NodeIndex(3)));
            Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String4", derivedPropertyNode.Retrieve(new NodeIndex(2)));
            Assert.Equal("String1", subDerivedPropertyNode.Retrieve(new NodeIndex(0)));
            Assert.Equal("String2", subDerivedPropertyNode.Retrieve(new NodeIndex(1)));
            Assert.Equal("String4", subDerivedPropertyNode.Retrieve(new NodeIndex(2)));
            Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(3)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(OverrideType.Base, subDerivedPropertyNode.GetContentOverride());
            Assert.Equal(OverrideType.Base, subDerivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
            Assert.Equal(OverrideType.Base, subDerivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
            Assert.Equal(OverrideType.Base, subDerivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
            Assert.Equal(4, baseIds.KeyCount);
            Assert.Equal(0, baseIds.DeletedCount);
            Assert.Equal(3, derivedIds.KeyCount);
            Assert.Equal(1, derivedIds.DeletedCount);
            Assert.Equal(3, subDerivedIds.KeyCount);
            Assert.Equal(0, subDerivedIds.DeletedCount);
            Assert.Equal(baseIds[0], derivedIds[0]);
            Assert.Equal(baseIds[1], derivedIds[1]);
            Assert.Equal(baseIds[3], derivedIds[2]);
            Assert.Equal(derivedIds[0], subDerivedIds[0]);
            Assert.Equal(derivedIds[1], subDerivedIds[1]);
            Assert.Equal(derivedIds[2], subDerivedIds[2]);
            Assert.True(derivedIds.IsDeleted(derivedDeletedId));
        }

        private static void AssertCollection(IGraphNode node, params string[] items)
        {
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                Assert.Equal(item, node.Retrieve(new NodeIndex(i)));
            }
        }
    }
}
