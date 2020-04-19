// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum.Tests
{
    public class TestObjectReferenceGraph
    {
        [Fact]
        public void TestSimpleObjectReferenceGraph()
        {
            var obj = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "MyInstance" };
            var asset = new Types.MyAssetWithRef2 { NonReference = obj, Reference = obj };
            var context = DeriveAssetTest<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            Assert.Equal(Types.MyAssetWithRef2.MemberCount, context.BaseGraph.RootNode.Members.Count);
            Assert.True(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].IsReference);
            Assert.Equal(obj, context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].Target.Retrieve());
            Assert.True(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].IsReference);
            Assert.Equal(obj, context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].Target.Retrieve());

            Assert.True(context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].IsReference);
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)], context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].BaseNode);
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].Target, context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].Target.BaseNode);
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)], context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].BaseNode);
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].Target, context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].Target.BaseNode);
        }

        [Fact]
        public void TestUpdateObjectReferenceGraph()
        {
            var obj = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "MyInstance" };
            var asset = new Types.MyAssetWithRef2 { NonReference = obj, Reference = obj };
            var context = DeriveAssetTest<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            Assert.Equal(Types.MyAssetWithRef2.MemberCount, context.BaseGraph.RootNode.Members.Count);
            Assert.True(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].IsReference);
            Assert.Equal(obj, context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].Target.Retrieve());
            Assert.True(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].IsReference);
            Assert.Equal(obj, context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].Target.Retrieve());

            Assert.True(context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].IsReference);
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)], context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].BaseNode);
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].Target, context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].Target.BaseNode);
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)], context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].BaseNode);
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].Target, context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].Target.BaseNode);
            // TODO
        }

        [Fact]
        public void TestCollectionObjectReferenceGraph()
        {
            var obj = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "MyInstance" };
            var asset = new Types.MyAssetWithRef2 { NonReference = obj, References = { obj } };
            var context = DeriveAssetTest<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>.DeriveAsset(asset);
            Assert.Equal(Types.MyAssetWithRef2.MemberCount, context.BaseGraph.RootNode.Members.Count);
            Assert.True(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].IsReference);
            Assert.Equal(obj, context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].Target.Retrieve());
            Assert.True(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.References)].IsReference);
            Assert.Equal(obj, context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.References)].Target.Retrieve(new NodeIndex(0)));
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].Target, context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.References)].Target.IndexedTarget(new NodeIndex(0)));

            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.References)], context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.References)].BaseNode);
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.References)].Target, context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.References)].Target.BaseNode);
            Assert.Equal(context.BaseGraph.RootNode[nameof(Types.MyAssetWithRef2.NonReference)].Target, context.DerivedGraph.RootNode[nameof(Types.MyAssetWithRef2.References)].Target.IndexedTarget(new NodeIndex(0)).BaseNode);
        }
    }
}
