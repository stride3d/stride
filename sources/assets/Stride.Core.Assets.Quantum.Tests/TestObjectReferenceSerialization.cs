// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using Xunit;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Quantum;

// ReSharper disable ConvertToLambdaExpression

namespace Stride.Core.Assets.Quantum.Tests
{
    public class TestObjectReferenceSerialization
    {
        private const string SimpleReferenceYaml = @"!Stride.Core.Assets.Quantum.Tests.Helpers.Types+MyAssetWithRef,Stride.Core.Assets.Quantum.Tests
Id: 00000001-0001-0000-0100-000001000000
Tags: []
MyObject1:
    Value: MyInstance
    Id: 00000002-0002-0000-0200-000002000000
MyObject2: ref!! 00000002-0002-0000-0200-000002000000
MyObjects: {}
MyNonIdObjects: []
";

        [Fact]
        public void TestSimpleReference()
        {
            Types.AssetWithRefPropertyGraphDefinition.IsObjectReferenceFunc = (targetNode, index) =>
            {
                return (targetNode as IMemberNode)?.Name == nameof(Types.MyAssetWithRef.MyObject2);
            };
            var obj = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "MyInstance" };
            var asset = new Types.MyAssetWithRef { MyObject1 = obj, MyObject2 = obj };
            var context = new AssetTestContainer<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>(asset);
            context.BuildGraph();
            SerializationHelper.SerializeAndCompare(context.AssetItem, context.Graph, SimpleReferenceYaml, false);

            context = AssetTestContainer<Types.MyAssetWithRef, Types.MyAssetBasePropertyGraph>.LoadFromYaml(SimpleReferenceYaml);
            Assert.Equal(context.Asset.MyObject1, context.Asset.MyObject2);
            Assert.Equal(GuidGenerator.Get(2), context.Asset.MyObject1.Id);
        }
    }
}
