// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NUnit.Framework;
using Xenko.Core.Assets.Quantum.Tests.Helpers;
using Xenko.Core.Assets.Tests.Helpers;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Quantum.Tests
{
    [TestFixture]
    public class TestReconcileObjectReferencesWithBase
    {
        [Test]
        public void TestWithCorrectObjectReferences()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
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
            Assert.AreEqual(GuidGenerator.Get(2), context.BaseAsset.MyObject1.Id);
            Assert.AreEqual(context.BaseAsset.MyObject1, context.BaseAsset.MyObject2);
            Assert.AreEqual(GuidGenerator.Get(3), context.DerivedAsset.MyObject1.Id);
            Assert.AreEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
            Assert.AreEqual(prevInstance, context.DerivedAsset.MyObject2);
        }

        [Test]
        public void TestWithIncorrectObjectReferences()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
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
            Assert.AreNotEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
            context.DerivedGraph.ReconcileWithBase();
            Assert.AreNotEqual(prevInstance, context.DerivedAsset.MyObject2);
            Assert.AreNotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.AreNotEqual(context.BaseAsset.MyObject2, context.DerivedAsset.MyObject2);
            Assert.AreEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
            Assert.Null(context.DerivedAsset.MyObject3);
        }

        [Test]
        public void TestWithOverriddenObjectReferences()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
MyObject3:
    Value: MyInstance
    Id: 00000003-0003-0003-0300-000003000000
";
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
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
            Assert.AreEqual(context.DerivedAsset.MyObject3, context.DerivedAsset.MyObject2);
            context.DerivedGraph.ReconcileWithBase();
            Assert.AreEqual(prevInstance, context.DerivedAsset.MyObject2);
            Assert.AreNotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.AreNotEqual(context.BaseAsset.MyObject2, context.DerivedAsset.MyObject2);
            Assert.AreNotEqual(context.BaseAsset.MyObject3, context.DerivedAsset.MyObject3);
            Assert.AreNotEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
            Assert.AreEqual(context.DerivedAsset.MyObject3, context.DerivedAsset.MyObject2);
            Assert.AreEqual(GuidGenerator.Get(4), context.DerivedAsset.MyObject3.Id);
        }

        [Test]
        public void TestWithInvalidObjectReferencesAndMissingTarget()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
MyObject1: null
MyObject2: ref!! 00000004-0004-0004-0400-000004000000
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IMemberNode)?.Name == nameof(Types.MyAssetWithRef.MyObject2);
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            var prevInstance = context.DerivedAsset.MyObject2;
            context.DerivedGraph.ReconcileWithBase();
            Assert.AreNotEqual(prevInstance, context.DerivedAsset.MyObject2);
            Assert.AreNotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.AreNotEqual(context.BaseAsset.MyObject2, context.DerivedAsset.MyObject2);
            Assert.NotNull(context.DerivedAsset.MyObject1);
            Assert.AreEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
        }

        [Test]
        public void TestWithCorrectObjectReferencesInList()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObjects:
    0a0000000a0000000a0000000a000000: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
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
            Assert.AreEqual(GuidGenerator.Get(2), context.BaseAsset.MyObject1.Id);
            Assert.AreEqual(context.BaseAsset.MyObject1, context.BaseAsset.MyObjects[0]);
            Assert.AreEqual(GuidGenerator.Get(3), context.DerivedAsset.MyObject1.Id);
            Assert.AreEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObjects[0]);
            Assert.AreEqual(prevInstance, context.DerivedAsset.MyObjects[0]);
        }

        [Test]
        public void TestWithIncorrectObjectReferencesInList()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObjects:
    0a0000000a0000000a0000000a000000: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
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
            Assert.AreNotEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObjects[0]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.AreNotEqual(prevInstance, context.DerivedAsset.MyObjects[0]);
            Assert.AreNotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.AreNotEqual(context.BaseAsset.MyObjects[0], context.DerivedAsset.MyObjects[0]);
            Assert.AreEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObjects[0]);
            Assert.Null(context.DerivedAsset.MyObject2);
        }

        [Test]
        public void TestWithOverriddenObjectReferencesInList()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
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
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
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
            Assert.AreEqual(context.DerivedAsset.MyObject2, context.DerivedAsset.MyObjects[0]);
            context.DerivedGraph.ReconcileWithBase();
            Assert.AreEqual(prevInstance, context.DerivedAsset.MyObjects[0]);
            Assert.AreNotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.AreNotEqual(context.BaseAsset.MyObjects[0], context.DerivedAsset.MyObjects[0]);
            Assert.AreNotEqual(context.BaseAsset.MyObject2, context.DerivedAsset.MyObject2);
            Assert.AreNotEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObjects[0]);
            Assert.AreEqual(context.DerivedAsset.MyObject2, context.DerivedAsset.MyObjects[0]);
            Assert.AreEqual(GuidGenerator.Get(4), context.DerivedAsset.MyObject2.Id);
        }

        [Test]
        public void TestWithInvalidObjectReferencesAndMissingTargetInList()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObjects:
    0a0000000a0000000a0000000a000000: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
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
            Assert.AreNotEqual(prevInstance, context.DerivedAsset.MyObjects[0]);
            Assert.AreNotEqual(context.BaseAsset.MyObject1, context.DerivedAsset.MyObject1);
            Assert.AreNotEqual(context.BaseAsset.MyObjects[0], context.DerivedAsset.MyObjects[0]);
            Assert.NotNull(context.DerivedAsset.MyObject1);
            Assert.AreEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObjects[0]);
        }

        [Test]
        public void TestAllMissing()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IMemberNode)?.Name == nameof(Types.MyAssetWithRef.MyObject2);
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            context.DerivedGraph.ReconcileWithBase();
            Assert.AreEqual(GuidGenerator.Get(2), context.BaseAsset.MyObject1.Id);
            Assert.AreEqual(context.BaseAsset.MyObject1, context.BaseAsset.MyObject2);
            Assert.AreNotEqual(GuidGenerator.Get(2), context.DerivedAsset.MyObject1.Id);
            Assert.AreEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
        }

        [Test]
        public void TestAllMissingInvertOrder()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObject1: ref!! 00000002-0002-0000-0200-000002000000
MyObject2:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => (targetNode as IMemberNode)?.Name == nameof(Types.MyAssetWithRef.MyObject1);
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            context.DerivedGraph.ReconcileWithBase();
            Assert.AreEqual(GuidGenerator.Get(2), context.BaseAsset.MyObject1.Id);
            Assert.AreEqual(context.BaseAsset.MyObject1, context.BaseAsset.MyObject2);
            Assert.AreNotEqual(GuidGenerator.Get(2), context.DerivedAsset.MyObject1.Id);
            Assert.AreEqual(context.DerivedAsset.MyObject1, context.DerivedAsset.MyObject2);
        }

        [Test]
        public void TestAllMissingInList()
        {
            const string baseYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
MyObjects:
    0a0000000a0000000a0000000a000000:
        Value: MyInstance
        Id: 00000002-0002-0000-0200-000002000000
    0a0000000b0000000b0000000b000000: ref!! 00000002-0002-0000-0200-000002000000
";
            const string derivedYaml = @"!Xenko.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Xenko.Core.Assets.Quantum.Tests
Id: 20000000-0000-0000-0000-000000000000
Archetype: 00000001-0001-0000-0100-000001000000:MyAsset
";
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) => index.IsInt && index.Int == 1;
            var context = DeriveAssetTest<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(baseYaml, derivedYaml);
            context.DerivedGraph.ReconcileWithBase();
            Assert.AreEqual(GuidGenerator.Get(2), context.BaseAsset.MyObjects[0].Id);
            Assert.AreEqual(context.BaseAsset.MyObjects[1], context.BaseAsset.MyObjects[0]);
            Assert.AreEqual(2, context.DerivedAsset.MyObjects.Count);
            Assert.AreNotEqual(GuidGenerator.Get(2), context.DerivedAsset.MyObjects[0].Id);
            Assert.AreEqual(context.DerivedAsset.MyObjects[1], context.DerivedAsset.MyObjects[0]);
        }

    }
}
