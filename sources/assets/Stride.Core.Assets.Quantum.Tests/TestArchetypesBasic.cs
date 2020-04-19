// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Xunit;
using Stride.Core.Assets.Quantum.Internal;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum.Tests
{
    public abstract class TestArchetypesRun
    {
        public static TestArchetypesRun Create<TAsset, TAssetPropertyGraph>(DeriveAssetTest<TAsset, TAssetPropertyGraph> context) where TAsset : Asset where TAssetPropertyGraph : AssetPropertyGraph
        {
            return new TestArchetypesRun<TAsset, TAssetPropertyGraph>(context);
        }

        public Action InitialCheck { get; set; }
        public Action FirstChange { get; set; }
        public Action FirstChangeCheck { get; set; }
        public Action SecondChange { get; set; }
        public Action SecondChangeCheck { get; set; }
    }

    public class TestArchetypesRun<TAsset, TAssetPropertyGraph> : TestArchetypesRun where TAsset : Asset where TAssetPropertyGraph : AssetPropertyGraph
    {
        public TestArchetypesRun(DeriveAssetTest<TAsset, TAssetPropertyGraph> context)
        {
            Context = context;
        }

        public DeriveAssetTest<TAsset, TAssetPropertyGraph> Context { get; set; }
    }

    public class TestArchetypesBasic
    {
        private static void RunTest(TestArchetypesRun run)
        {
            run.InitialCheck();
            run.FirstChange();
            run.FirstChangeCheck();
            run.SecondChange();
            run.SecondChangeCheck();
        }

        [Fact]
        public void TestSimplePropertyChange()
        {
            RunTest(PrepareSimplePropertyChange());
        }

        public static TestArchetypesRun PrepareSimplePropertyChange()
        {
            var asset = new Types.MyAsset1 { MyString = "String" };
            var context = DeriveAssetTest<Types.MyAsset1, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset1.MyString)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset1.MyString)];

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                // Initial checks
                Assert.Equal("String", basePropertyNode.Retrieve());
                Assert.Equal("String", derivedPropertyNode.Retrieve());
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            };
            test.FirstChange = () => { basePropertyNode.Update("MyBaseString"); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal("MyBaseString", basePropertyNode.Retrieve());
                Assert.Equal("MyBaseString", derivedPropertyNode.Retrieve());
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
            };
            test.SecondChange = () => { derivedPropertyNode.Update("MyDerivedString"); };
            test.SecondChangeCheck = () =>
            {
                Assert.Equal("MyBaseString", basePropertyNode.Retrieve());
                Assert.Equal("MyDerivedString", derivedPropertyNode.Retrieve());
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.New, derivedPropertyNode.GetContentOverride());
            };
            return test;
        }

        [Fact]
        public void TestAbstractPropertyChange()
        {
            RunTest(PrepareAbstractPropertyChange());
        }

        public static TestArchetypesRun PrepareAbstractPropertyChange()
        {
            var asset = new Types.MyAsset5 { MyInterface = new Types.SomeObject2 { Value = "String1" } };
            var context = DeriveAssetTest<Types.MyAsset5, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset5.MyInterface)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset5.MyInterface)];

            var objB = asset.MyInterface;
            var objD = context.DerivedAsset.MyInterface;
            var newObjB = new Types.SomeObject { Value = "MyBaseString" };
            var newObjD = new Types.SomeObject2 { Value = "MyDerivedString" };

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(objB, basePropertyNode.Retrieve());
                // NOTE: we're using this code to test undo/redo and in this case, we have different objects in the derived object after undoing due to the fact that the type of the instance has changed
                //Assert.Equal(objD, derivedPropertyNode.Content.Retrieve());
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve()).Value);
                Assert.Equal("String1", ((Types.IMyInterface)derivedPropertyNode.Retrieve()).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target[nameof(Types.SomeObject.Value)].GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target[nameof(Types.SomeObject.Value)].GetContentOverride());
            };
            test.FirstChange = () => { basePropertyNode.Update(newObjB); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(newObjB, basePropertyNode.Retrieve());
                Assert.NotEqual(objD, derivedPropertyNode.Retrieve());
                Assert.Equal("MyBaseString", ((Types.IMyInterface)basePropertyNode.Retrieve()).Value);
                Assert.Equal("MyBaseString", ((Types.IMyInterface)derivedPropertyNode.Retrieve()).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target[nameof(Types.SomeObject.Value)].GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target[nameof(Types.SomeObject.Value)].GetContentOverride());
            };
            test.SecondChange = () => { derivedPropertyNode.Update(newObjD); };
            test.SecondChangeCheck = () =>
            {
                Assert.Equal(newObjB, basePropertyNode.Retrieve());
                Assert.Equal(newObjD, derivedPropertyNode.Retrieve());
                Assert.Equal("MyBaseString", ((Types.IMyInterface)basePropertyNode.Retrieve()).Value);
                Assert.Equal("MyDerivedString", ((Types.IMyInterface)derivedPropertyNode.Retrieve()).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target[nameof(Types.SomeObject.Value)].GetContentOverride());
                Assert.Equal(OverrideType.New, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target[nameof(Types.SomeObject2.Value)].GetContentOverride());
            };
            return test;
        }

        [Fact]
        public void TestSimpleCollectionUpdate()
        {
            RunTest(PrepareSimpleCollectionUpdate());
        }

        public static TestArchetypesRun PrepareSimpleCollectionUpdate()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2" } };
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyStrings.Count);
                Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.FirstChange = () => { basePropertyNode.Target.Update("MyBaseString", new NodeIndex(1)); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyStrings.Count);
                Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("MyBaseString", basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("MyBaseString", derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.SecondChange = () => { derivedPropertyNode.Target.Update("MyDerivedString", new NodeIndex(0)); };
            test.SecondChangeCheck = () =>
            {
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
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            return test;
        }

        [Fact]
        public void TestSimpleCollectionAdd()
        {
            RunTest(PrepareSimpleCollectionAdd());
        }

        public static TestArchetypesRun PrepareSimpleCollectionAdd()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2" } };
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyStrings.Count);
                Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.FirstChange = () => { derivedPropertyNode.Target.Add("String3"); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyStrings.Count);
                Assert.Equal(3, context.DerivedAsset.MyStrings.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String3", derivedPropertyNode.Retrieve(new NodeIndex(2)));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(3, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.SecondChange = () => { basePropertyNode.Target.Add("String4"); };
            test.SecondChangeCheck = () =>
            {
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
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.Equal(3, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(4, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
                Assert.Equal(baseIds[2], derivedIds[2]);
            };
            return test;
        }

        [Fact]
        public void TestSimpleCollectionRemove()
        {
            RunTest(PrepareSimpleCollectionRemove());
        }

        public static TestArchetypesRun PrepareSimpleCollectionRemove()
        {
            var asset = new Types.MyAsset2 { MyStrings = { "String1", "String2", "String3", "String4" } };
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyStrings);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.MyStrings)];
            ItemId derivedDeletedId = ItemId.Empty;
            ItemId baseDeletedId = ItemId.Empty;

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(4, context.BaseAsset.MyStrings.Count);
                Assert.Equal(4, context.DerivedAsset.MyStrings.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String3", basePropertyNode.Retrieve(new NodeIndex(2)));
                Assert.Equal("String4", basePropertyNode.Retrieve(new NodeIndex(3)));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String3", derivedPropertyNode.Retrieve(new NodeIndex(2)));
                Assert.Equal("String4", derivedPropertyNode.Retrieve(new NodeIndex(3)));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(2)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(3)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(3)));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(4, baseIds.KeyCount);
                Assert.Equal(4, derivedIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
                Assert.Equal(baseIds[2], derivedIds[2]);
                Assert.Equal(baseIds[3], derivedIds[3]);
            };
            test.FirstChange = () =>
            {
                derivedDeletedId = derivedIds[2];
                derivedPropertyNode.Target.Remove("String3", new NodeIndex(2));
            };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(4, context.BaseAsset.MyStrings.Count);
                Assert.Equal(3, context.DerivedAsset.MyStrings.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String3", basePropertyNode.Retrieve(new NodeIndex(2)));
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
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(4, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(3, derivedIds.KeyCount);
                Assert.Equal(1, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
                Assert.Equal(baseIds[3], derivedIds[2]);
                Assert.True(derivedIds.IsDeleted(derivedDeletedId));
            };
            test.SecondChange = () =>
            {
                baseDeletedId = baseIds[3];
                basePropertyNode.Target.Remove("String4", new NodeIndex(3));
            };
            test.SecondChangeCheck = () =>
            {
                Assert.Equal(3, context.BaseAsset.MyStrings.Count);
                Assert.Equal(2, context.DerivedAsset.MyStrings.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String3", basePropertyNode.Retrieve(new NodeIndex(2)));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(2)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.Equal(3, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(1, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
                Assert.True(derivedIds.IsDeleted(derivedDeletedId));
                Assert.True(!baseIds.IsDeleted(baseDeletedId));
            };
            return test;
        }

        [Fact]
        public void TestCollectionInStructUpdate()
        {
            RunTest(PrepareCollectionInStructUpdate());
        }

        public static TestArchetypesRun PrepareCollectionInStructUpdate()
        {
            var asset = new Types.MyAsset2();
            asset.Struct.MyStrings.Add("String1");
            asset.Struct.MyStrings.Add("String2");
            var context = DeriveAssetTest<Types.MyAsset2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.Struct.MyStrings);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.Struct.MyStrings);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset2.Struct)].Target[nameof(Types.MyAsset2.MyStrings)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset2.Struct)].Target[nameof(Types.MyAsset2.MyStrings)];

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.Struct.MyStrings.Count);
                Assert.Equal(2, context.DerivedAsset.Struct.MyStrings.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.FirstChange = () => { basePropertyNode.Target.Update("MyBaseString", new NodeIndex(1)); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.Struct.MyStrings.Count);
                Assert.Equal(2, context.DerivedAsset.Struct.MyStrings.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("MyBaseString", basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal("MyBaseString", derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.SecondChange = () => { derivedPropertyNode.Target.Update("MyDerivedString", new NodeIndex(0)); };
            test.SecondChangeCheck = () =>
            {
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
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            return test;
        }

        [Fact]
        public void TestSimpleDictionaryUpdate()
        {
            RunTest(PrepareSimpleDictionaryUpdate());
        }

        public static TestArchetypesRun PrepareSimpleDictionaryUpdate()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            };
            test.FirstChange = () => { basePropertyNode.Target.Update("MyBaseString", new NodeIndex("Key2")); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("MyBaseString", basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("MyBaseString", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            };
            test.SecondChange = () => { derivedPropertyNode.Target.Update("MyDerivedString", new NodeIndex("Key1")); };
            test.SecondChangeCheck = () =>
            {
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
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            };
            return test;
        }

        [Fact]
        public void TestSimpleDictionaryAdd()
        {
            RunTest(PrepareSimpleDictionaryAdd());
        }

        public static TestArchetypesRun PrepareSimpleDictionaryAdd()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            };
            test.FirstChange = () => { derivedPropertyNode.Target.Add("String3", new NodeIndex("Key3")); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(3, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String3", derivedPropertyNode.Retrieve(new NodeIndex("Key3")));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key3")));
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(3, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
                Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(3, context.DerivedAsset.MyDictionary.Count);
            };
            test.SecondChange = () => { basePropertyNode.Target.Add("String4", new NodeIndex("Key4")); };
            test.SecondChangeCheck = () =>
            {
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
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
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
            };
            return test;
        }

        [Fact]
        public void TestSimpleDictionaryRemove()
        {
            RunTest(PrepareSimpleDictionaryRemove());
        }

        public static TestArchetypesRun PrepareSimpleDictionaryRemove()
        {
            var asset = new Types.MyAsset3 { MyDictionary = { { "Key1", "String1" }, { "Key2", "String2" }, { "Key3", "String3" }, { "Key4", "String4" } } };
            var context = DeriveAssetTest<Types.MyAsset3, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(context.BaseAsset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset3.MyDictionary)];
            ItemId derivedDeletedId = ItemId.Empty;
            ItemId baseDeletedId = ItemId.Empty;

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(4, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(4, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String3", basePropertyNode.Retrieve(new NodeIndex("Key3")));
                Assert.Equal("String4", basePropertyNode.Retrieve(new NodeIndex("Key4")));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String3", derivedPropertyNode.Retrieve(new NodeIndex("Key3")));
                Assert.Equal("String4", derivedPropertyNode.Retrieve(new NodeIndex("Key4")));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key3")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key4")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key3")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key4")));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(4, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(4, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
                Assert.Equal(baseIds["Key3"], derivedIds["Key3"]);
                Assert.Equal(baseIds["Key4"], derivedIds["Key4"]);
            };
            test.FirstChange = () =>
            {
                derivedDeletedId = derivedIds["Key3"];
                derivedPropertyNode.Target.Remove("String3", new NodeIndex("Key3"));
            };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(4, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(3, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String3", basePropertyNode.Retrieve(new NodeIndex("Key3")));
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
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(4, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(3, derivedIds.KeyCount);
                Assert.Equal(1, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
                Assert.Equal(baseIds["Key4"], derivedIds["Key4"]);
                Assert.True(derivedIds.IsDeleted(derivedDeletedId));
            };
            test.SecondChange = () =>
            {
                baseDeletedId = baseIds["Key4"];
                basePropertyNode.Target.Remove("String4", new NodeIndex("Key4"));
            };
            test.SecondChangeCheck = () =>
            {
                Assert.Equal(3, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal("String1", basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String3", basePropertyNode.Retrieve(new NodeIndex("Key3")));
                Assert.Equal("String1", derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal("String2", derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key3")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.Equal(3, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(1, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
                Assert.True(derivedIds.IsDeleted(derivedDeletedId));
                Assert.True(!baseIds.IsDeleted(baseDeletedId));
            };
            return test;
        }

        [Fact]
        public void TestObjectCollectionUpdate()
        {
            RunTest(PrepareObjectCollectionUpdate());
        }

        public static TestArchetypesRun PrepareObjectCollectionUpdate()
        {
            var asset = new Types.MyAsset4 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset4, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyObjects);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyObjects);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];

            var objB0 = asset.MyObjects[0];
            var objB1 = asset.MyObjects[1];
            var objD0 = context.DerivedAsset.MyObjects[0];
            var objD1 = context.DerivedAsset.MyObjects[1];
            var newObjB = new Types.SomeObject { Value = "MyBaseString" };
            var newObjD = new Types.SomeObject { Value = "MyDerivedString" };

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyObjects.Count);
                Assert.Equal(2, context.DerivedAsset.MyObjects.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String1", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.FirstChange = () => { basePropertyNode.Target.Update(newObjB, new NodeIndex(1)); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyObjects.Count);
                Assert.Equal(2, context.DerivedAsset.MyObjects.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(newObjB, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("MyBaseString", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String1", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("MyBaseString", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.SecondChange = () => { derivedPropertyNode.Target.Update(newObjD, new NodeIndex(0)); };
            test.SecondChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyObjects.Count);
                Assert.Equal(2, context.DerivedAsset.MyObjects.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(newObjB, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(newObjD, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
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
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            return test;
        }

        [Fact]
        public void TestObjectCollectionAdd()
        {
            RunTest(PrepareObjectCollectionAdd());
        }

        public static TestArchetypesRun PrepareObjectCollectionAdd()
        {
            var asset = new Types.MyAsset4 { MyObjects = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset4, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyObjects);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyObjects);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset4.MyObjects)];

            var objB0 = asset.MyObjects[0];
            var objB1 = asset.MyObjects[1];
            var objD0 = context.DerivedAsset.MyObjects[0];
            var objD1 = context.DerivedAsset.MyObjects[1];
            var newObjD = new Types.SomeObject { Value = "String3" };
            var newObjB = new Types.SomeObject { Value = "String4" };

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyObjects.Count);
                Assert.Equal(2, context.DerivedAsset.MyObjects.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String1", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.FirstChange = () => { derivedPropertyNode.Target.Add(newObjD); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyObjects.Count);
                Assert.Equal(3, context.DerivedAsset.MyObjects.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(newObjD, derivedPropertyNode.Retrieve(new NodeIndex(2)));
                Assert.Equal("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String1", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String3", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(2))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(2)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(3, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.SecondChange = () => { basePropertyNode.Target.Add(newObjB); };
            test.SecondChangeCheck = () =>
            {
                Assert.Equal(3, context.BaseAsset.MyObjects.Count);
                Assert.Equal(4, context.DerivedAsset.MyObjects.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(newObjB, basePropertyNode.Retrieve(new NodeIndex(2)));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.NotEqual(newObjB, derivedPropertyNode.Retrieve(new NodeIndex(2)));
                Assert.Equal(newObjD, derivedPropertyNode.Retrieve(new NodeIndex(3)));
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
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(3, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(4, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
                Assert.Equal(baseIds[2], derivedIds[2]);
            };
            return test;
        }

        [Fact]
        public void TestAbstractCollectionUpdate()
        {
            RunTest(PrepareAbstractCollectionUpdate());
        }

        public static TestArchetypesRun PrepareAbstractCollectionUpdate()
        {
            var asset = new Types.MyAsset5 { MyInterfaces = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject2 { Value = "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset5, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyInterfaces);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyInterfaces);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset5.MyInterfaces)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset5.MyInterfaces)];

            var objB0 = asset.MyInterfaces[0];
            var objB1 = asset.MyInterfaces[1];
            var objD0 = context.DerivedAsset.MyInterfaces[0];
            var objD1 = context.DerivedAsset.MyInterfaces[1];
            var newObjB = new Types.SomeObject { Value = "MyBaseString" };
            var newObjD = new Types.SomeObject2 { Value = "MyDerivedString" };

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyInterfaces.Count);
                Assert.Equal(2, context.DerivedAsset.MyInterfaces.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex(1)));
                // NOTE: we're using this code to test undo/redo and in this case, we have different objects in the derived object after undoing due to the fact that the type of the instance has changed
                //Assert.Equal(objD0, derivedPropertyNode.Content.Retrieve(new Index(0)));
                //Assert.Equal(objD1, derivedPropertyNode.Content.Retrieve(new Index(1)));
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String1", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.FirstChange = () => { basePropertyNode.Target.Update(newObjB, new NodeIndex(1)); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyInterfaces.Count);
                Assert.Equal(2, context.DerivedAsset.MyInterfaces.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(newObjB, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.NotEqual(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("MyBaseString", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String1", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("MyBaseString", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.SecondChange = () => { derivedPropertyNode.Target.Update(newObjD, new NodeIndex(0)); };
            test.SecondChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyInterfaces.Count);
                Assert.Equal(2, context.DerivedAsset.MyInterfaces.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(newObjB, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(newObjD, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.NotEqual(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("MyBaseString", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("MyDerivedString", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("MyBaseString", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
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
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            return test;
        }

        [Fact]
        public void TestAbstractCollectionAdd()
        {
            RunTest(PrepareAbstractCollectionAdd());
        }

        public static TestArchetypesRun PrepareAbstractCollectionAdd()
        {
            var asset = new Types.MyAsset5 { MyInterfaces = { new Types.SomeObject { Value = "String1" }, new Types.SomeObject { Value = "String2" } } };
            var context = DeriveAssetTest<Types.MyAsset5, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyInterfaces);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyInterfaces);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset5.MyInterfaces)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset5.MyInterfaces)];

            var objB0 = asset.MyInterfaces[0];
            var objB1 = asset.MyInterfaces[1];
            var objD0 = context.DerivedAsset.MyInterfaces[0];
            var objD1 = context.DerivedAsset.MyInterfaces[1];
            var newObjD = new Types.SomeObject { Value = "String3" };
            var newObjB = new Types.SomeObject2 { Value = "String4" };

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyInterfaces.Count);
                Assert.Equal(2, context.DerivedAsset.MyInterfaces.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String1", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.FirstChange = () => { derivedPropertyNode.Target.Add(newObjD); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyInterfaces.Count);
                Assert.Equal(3, context.DerivedAsset.MyInterfaces.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(newObjD, derivedPropertyNode.Retrieve(new NodeIndex(2)));
                Assert.Equal("String1", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.SomeObject)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String1", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String3", ((Types.SomeObject)derivedPropertyNode.Retrieve(new NodeIndex(2))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(2)].TargetNode[nameof(Types.SomeObject.Value)]).GetContentOverride());
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(3, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
            };
            test.SecondChange = () => { basePropertyNode.Target.Add(newObjB); };
            test.SecondChangeCheck = () =>
            {
                Assert.Equal(3, context.BaseAsset.MyInterfaces.Count);
                Assert.Equal(4, context.DerivedAsset.MyInterfaces.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex(1)));
                Assert.Equal(newObjB, basePropertyNode.Retrieve(new NodeIndex(2)));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex(0)));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex(1)));
                Assert.NotEqual(newObjB, derivedPropertyNode.Retrieve(new NodeIndex(2)));
                Assert.Equal(newObjD, derivedPropertyNode.Retrieve(new NodeIndex(3)));
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String4", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex(2))).Value);
                Assert.Equal("String1", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex(0))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex(1))).Value);
                Assert.Equal("String4", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex(2))).Value);
                Assert.Equal("String3", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex(3))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex(2)].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(0)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(1)));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(2)));
                Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex(3)));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(0)].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(1)].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(2)].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex(3)].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(3, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(4, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds[0], derivedIds[0]);
                Assert.Equal(baseIds[1], derivedIds[1]);
                Assert.Equal(baseIds[2], derivedIds[2]);
            };
            return test;
        }

        [Fact]
        public void TestAbstractDictionaryUpdate()
        {
            RunTest(PrepareAbstractDictionaryUpdate());
        }

        public static TestArchetypesRun PrepareAbstractDictionaryUpdate()
        {
            var asset = new Types.MyAsset6 { MyDictionary = { { "Key1", new Types.SomeObject { Value = "String1" } }, { "Key2", new Types.SomeObject2 { Value = "String2" } } } };
            var context = DeriveAssetTest<Types.MyAsset6, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset6.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset6.MyDictionary)];

            var objB0 = asset.MyDictionary["Key1"];
            var objB1 = asset.MyDictionary["Key2"];
            var objD0 = context.DerivedAsset.MyDictionary["Key1"];
            var objD1 = context.DerivedAsset.MyDictionary["Key2"];
            var newObjB = new Types.SomeObject { Value = "MyBaseString" };
            var newObjD = new Types.SomeObject2 { Value = "MyDerivedString" };

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex("Key2")));
                // NOTE: we're using this code to test undo/redo and in this case, we have different objects in the derived object after undoing due to the fact that the type of the instance has changed
                //Assert.Equal(objD0, derivedPropertyNode.Content.Retrieve(new Index("Key1")));
                //Assert.Equal(objD1, derivedPropertyNode.Content.Retrieve(new Index("Key2")));
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal("String1", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            };
            test.FirstChange = () => { basePropertyNode.Target.Update(newObjB, new NodeIndex("Key2")); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal(newObjB, basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.NotEqual(objD1, derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("MyBaseString", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal("String1", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("MyBaseString", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            };
            test.SecondChange = () => { derivedPropertyNode.Target.Update(newObjD, new NodeIndex("Key1")); };
            test.SecondChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal(newObjB, basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal(newObjD, derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.NotEqual(objD1, derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("MyBaseString", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal("MyDerivedString", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("MyBaseString", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            };
            return test;
        }

        [Fact]
        public void TestAbstractDictionaryAdd()
        {
            RunTest(PrepareAbstractDictionaryAdd());
        }

        public static TestArchetypesRun PrepareAbstractDictionaryAdd()
        {
            var asset = new Types.MyAsset6 { MyDictionary = { { "Key1", new Types.SomeObject { Value = "String1" } }, { "Key2", new Types.SomeObject2 { Value = "String2" } } } };
            var context = DeriveAssetTest<Types.MyAsset6, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            var baseIds = CollectionItemIdHelper.GetCollectionItemIds(asset.MyDictionary);
            var derivedIds = CollectionItemIdHelper.GetCollectionItemIds(context.DerivedAsset.MyDictionary);
            var basePropertyNode = context.BaseGraph.RootNode[nameof(Types.MyAsset6.MyDictionary)];
            var derivedPropertyNode = context.DerivedGraph.RootNode[nameof(Types.MyAsset6.MyDictionary)];

            var objB0 = asset.MyDictionary["Key1"];
            var objB1 = asset.MyDictionary["Key2"];
            var objD0 = context.DerivedAsset.MyDictionary["Key1"];
            var objD1 = context.DerivedAsset.MyDictionary["Key2"];
            var newObjD = new Types.SomeObject { Value = "String3" };
            var newObjB = new Types.SomeObject2 { Value = "String4" };

            var test = TestArchetypesRun.Create(context);
            test.InitialCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(2, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal("String1", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(2, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            };
            test.FirstChange = () => { derivedPropertyNode.Target.Add(newObjD, new NodeIndex("Key3")); };
            test.FirstChangeCheck = () =>
            {
                Assert.Equal(2, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(3, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal(newObjD, derivedPropertyNode.Retrieve(new NodeIndex("Key3")));
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal("String1", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal("String3", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key3"))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key3")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key3")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(2, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(3, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
            };
            test.SecondChange = () => { basePropertyNode.Target.Add(newObjB, new NodeIndex("Key4")); };
            test.SecondChangeCheck = () =>
            {
                Assert.Equal(3, context.BaseAsset.MyDictionary.Count);
                Assert.Equal(4, context.DerivedAsset.MyDictionary.Count);
                Assert.Equal(objB0, basePropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal(objB1, basePropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.Equal(newObjB, basePropertyNode.Retrieve(new NodeIndex("Key4")));
                Assert.Equal(objD0, derivedPropertyNode.Retrieve(new NodeIndex("Key1")));
                Assert.Equal(objD1, derivedPropertyNode.Retrieve(new NodeIndex("Key2")));
                Assert.NotEqual(newObjB, derivedPropertyNode.Retrieve(new NodeIndex("Key4")));
                Assert.Equal(newObjD, derivedPropertyNode.Retrieve(new NodeIndex("Key3")));
                Assert.Equal("String1", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal("String4", ((Types.IMyInterface)basePropertyNode.Retrieve(new NodeIndex("Key4"))).Value);
                Assert.Equal("String1", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key1"))).Value);
                Assert.Equal("String2", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key2"))).Value);
                Assert.Equal("String4", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key4"))).Value);
                Assert.Equal("String3", ((Types.IMyInterface)derivedPropertyNode.Retrieve(new NodeIndex("Key3"))).Value);
                Assert.Equal(OverrideType.Base, basePropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, basePropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)basePropertyNode.Target.ItemReferences[new NodeIndex("Key4")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.GetContentOverride());
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key1")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key2")));
                Assert.Equal(OverrideType.New, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key3")));
                Assert.Equal(OverrideType.Base, derivedPropertyNode.Target.GetItemOverride(new NodeIndex("Key4")));
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key1")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key2")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key3")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Equal(OverrideType.Base, ((AssetMemberNode)derivedPropertyNode.Target.ItemReferences[new NodeIndex("Key4")].TargetNode[nameof(Types.IMyInterface.Value)]).GetContentOverride());
                Assert.Single(derivedPropertyNode.Target.GetOverriddenItemIndices());
                Assert.Empty(derivedPropertyNode.Target.GetOverriddenKeyIndices());
                Assert.NotSame(baseIds, derivedIds);
                Assert.Equal(3, baseIds.KeyCount);
                Assert.Equal(0, baseIds.DeletedCount);
                Assert.Equal(4, derivedIds.KeyCount);
                Assert.Equal(0, derivedIds.DeletedCount);
                Assert.Equal(baseIds["Key1"], derivedIds["Key1"]);
                Assert.Equal(baseIds["Key2"], derivedIds["Key2"]);
                Assert.Equal(baseIds["Key4"], derivedIds["Key4"]);
            };
            return test;
        }
    }
}
