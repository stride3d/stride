// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum.Tests
{
    public class TestReconcileObjectReferencesWithBase
    {
        [Fact]
        public void TestWithCorrectObjectReferences()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObject1:
    Value: MyModifiedInstance
    Id: 00000003-0003-0000-0300-000003000000
MyObject2: ref!! 00000003-0003-0000-0300-000003000000
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IMemberNode)?.Name == nameof(Types.MyAssetWithRef.MyObject2);
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var prevInstance = context.DerivedAsset.MyObject2;
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(GuidGenerator.Get(2), context.BaseAsset.MyObject1.Id);
            Assert.Equal(context.BaseAsset.MyObject1, context.BaseAsset.MyObject2);
            Assert.Equal(GuidGenerator.Get(3), context.DerivedAsset.MyObject1.Id);
            Assert.Equal(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
            Assert.Equal(prevInstance, context.DerivedAsset.MyObject2);
        }

        [Fact]
        public void TestWithIncorrectObjectReferences()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObject1:
    Value: MyModifiedInstance
    Id: 00000003-0003-0000-0300-000003000000
MyObject2: ref!! 00000004-0004-0000-0400-000004000000
MyObject3:
    Value: MyModifiedInstance
    Id: 00000004-0004-0000-0400-000004000000
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IMemberNode)?.Name == nameof(Types.MyAssetWithRef.MyObject2);
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var prevInstance = context.DerivedAsset.MyObject2;
            Assert.NotEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
            context.DerivedGraph.ReconcileWithBase();
            Assert.NotEqual(prevInstance, context.DerivedAsset.MyObject2);
            Assert.NotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.NotEqual(context.BaseAsset.MyObject2, context.DerivedAsset.MyObject2);
            Assert.Equal(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
            Assert.Null(context.DerivedAsset.MyObject3);
        }

        [Fact]
        public void TestWithOverriddenObjectReferences()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
MyObject3:
    Value: MyInstance
    Id: 00000003-0003-0003-0300-000003000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObject1:
    Value: MyModifiedInstance
    Id: 00000003-0003-0003-0300-000003000000
MyObject2*: ref!! 00000004-0004-0000-0400-000004000000
MyObject3:
    Value: MyInstance
    Id: 00000004-0004-0000-0400-000004000000
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IMemberNode)?.Name == nameof(Types.MyAssetWithRef.MyObject2);
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var prevInstance = context.DerivedAsset.MyObject2;
            Assert.Equal(context.DerivedAsset.MyObject3, context.DerivedAsset.MyObject2);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(prevInstance, context.DerivedAsset.MyObject2);
            Assert.NotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.NotEqual(context.BaseAsset.MyObject2, context.DerivedAsset.MyObject2);
            Assert.NotEqual(context.BaseAsset.MyObject3, context.DerivedAsset.MyObject3);
            Assert.NotEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
            Assert.Equal(context.DerivedAsset.MyObject3, context.DerivedAsset.MyObject2);
            Assert.Equal(GuidGenerator.Get(4), context.DerivedAsset.MyObject3.Id);
        }

        [Fact]
        public void TestWithInvalidObjectReferencesAndMissingTarget()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObject1: null
MyObject2: ref!! 00000004-0004-0004-0400-000004000000
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IMemberNode)?.Name == nameof(Types.MyAssetWithRef.MyObject2);
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var prevInstance = context.DerivedAsset.MyObject2;
            context.DerivedGraph.ReconcileWithBase();
            Assert.NotEqual(prevInstance, context.DerivedAsset.MyObject2);
            Assert.NotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.NotEqual(context.BaseAsset.MyObject2, context.DerivedAsset.MyObject2);
            Assert.NotNull(context.DerivedAsset.MyObject1);
            Assert.Equal(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
        }

        [Fact]
        public void TestWithCorrectObjectReferencesInList()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObjects:
    0a0000000a0000000a0000000a000000: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObject1:
    Value: MyInstance
    Id: 00000003-0003-0000-0300-000003000000
MyObjects:
    0a0000000a0000000a0000000a000000: ref!! 00000003-0003-0000-0300-000003000000
";

            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var prevInstance = context.DerivedAsset.MyObjects[0];
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(GuidGenerator.Get(2), context.BaseAsset.MyObject1.Id);
            Assert.Equal(context.BaseAsset.MyObject1, context.BaseAsset.MyObjects[0]);
            Assert.Equal(GuidGenerator.Get(3), context.DerivedAsset.MyObject1.Id);
            Assert.Equal(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObjects[0]);
            Assert.Equal(prevInstance, context.DerivedAsset.MyObjects[0]);
        }

        [Fact]
        public void TestWithIncorrectObjectReferencesInList()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObjects:
    0a0000000a0000000a0000000a000000: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObject1:
    Value: MyModifiedInstance
    Id: 00000003-0003-0000-0300-000003000000
MyObject2:
    Value: MyModifiedInstance
    Id: 00000004-0004-0000-0400-000004000000
MyObjects:
    0a0000000a0000000a0000000a000000: ref!! 00000004-0004-0000-0400-000004000000
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IObjectNode)?.ItemReferences != null;
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var prevInstance = context.DerivedAsset.MyObjects[0];
            Assert.NotEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObjects[0]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.NotEqual(prevInstance, context.DerivedAsset.MyObjects[0]);
            Assert.NotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.NotEqual(context.BaseAsset.MyObjects[0], context.DerivedAsset.MyObjects[0]);
            Assert.Equal(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObjects[0]);
            Assert.Null(context.DerivedAsset.MyObject2);
        }

        [Fact]
        public void TestWithOverriddenObjectReferencesInList()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2:
    Value: MyInstance
    Id: 00000003-0003-0003-0300-000003000000
MyObjects:
    0a0000000a0000000a0000000a000000: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObject1:
    Value: MyModifiedInstance
    Id: 00000003-0003-0003-0300-000003000000
MyObject2:
    Value: MyInstance
    Id: 00000004-0004-0000-0400-000004000000
MyObjects:
    0a0000000a0000000a0000000a000000*: ref!! 00000004-0004-0000-0400-000004000000
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IObjectNode)?.ItemReferences != null;
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var prevInstance = context.DerivedAsset.MyObjects[0];
            Assert.Equal(context.DerivedAsset.MyObject2, context.DerivedAsset.MyObjects[0]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(prevInstance, context.DerivedAsset.MyObjects[0]);
            Assert.NotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.NotEqual(context.BaseAsset.MyObjects[0], context.DerivedAsset.MyObjects[0]);
            Assert.NotEqual(context.BaseAsset.MyObject2, context.DerivedAsset.MyObject2);
            Assert.NotEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObjects[0]);
            Assert.Equal(context.DerivedAsset.MyObject2, context.DerivedAsset.MyObjects[0]);
            Assert.Equal(GuidGenerator.Get(4), context.DerivedAsset.MyObject2.Id);
        }

        [Fact]
        public void TestWithInvalidObjectReferencesAndMissingTargetInList()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObjects:
    0a0000000a0000000a0000000a000000: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObject1: null
MyObjects:
    0a0000000a0000000a0000000a000000: ref!! 00000002-0002-0000-0200-000002000000
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IObjectNode)?.ItemReferences != null;
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var prevInstance = context.DerivedAsset.MyObjects[0];
            context.DerivedGraph.ReconcileWithBase();
            Assert.NotEqual(prevInstance, context.DerivedAsset.MyObjects[0]);
            Assert.NotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.NotEqual(context.BaseAsset.MyObjects[0], context.DerivedAsset.MyObjects[0]);
            Assert.NotNull(context.DerivedAsset.MyObject1);
            Assert.Equal(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObjects[0]);
        }

        [Fact]
        public void TestAllMissing()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IMemberNode)?.Name == nameof(Types.MyAssetWithRef.MyObject2);
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(GuidGenerator.Get(2), context.BaseAsset.MyObject1.Id);
            Assert.Equal(context.BaseAsset.MyObject1, context.BaseAsset.MyObject2);
            Assert.NotEqual(GuidGenerator.Get(2), context.DerivedAsset.MyObject1.Id);
            Assert.Equal(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
        }

        [Fact]
        public void TestAllMissingInvertOrder()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1: ref!! 00000002-0002-0000-0200-000002000000
MyObject2:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IMemberNode)?.Name == nameof(Types.MyAssetWithRef.MyObject1);
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(GuidGenerator.Get(2), context.BaseAsset.MyObject1.Id);
            Assert.Equal(context.BaseAsset.MyObject1, context.BaseAsset.MyObject2);
            Assert.NotEqual(GuidGenerator.Get(2), context.DerivedAsset.MyObject1.Id);
            Assert.Equal(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
        }

        [Fact]
        public void TestAllMissingInList()
        {
            const string baseYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value: MyInstance
        Id: 00000002-0002-0000-0200-000002000000
    0a0000000b0000000b0000000b000000: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => index.IsInt && index.Int == 1;
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            context.DerivedGraph.ReconcileWithBase();
            Assert.Equal(GuidGenerator.Get(2), context.BaseAsset.MyObjects[0].Id);
            Assert.Equal(context.BaseAsset.MyObjects[1], context.BaseAsset.MyObjects[0]);
            Assert.Equal(2, context.DerivedAsset.MyObjects.Count);
            Assert.NotEqual(GuidGenerator.Get(2), context.DerivedAsset.MyObjects[0].Id);
            Assert.Equal(context.DerivedAsset.MyObjects[1], context.DerivedAsset.MyObjects[0]);
        }

    }
}
