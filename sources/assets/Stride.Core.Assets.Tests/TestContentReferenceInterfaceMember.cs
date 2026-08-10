// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets.Analysis;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Xunit;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Regression guard for asset-reference analysis over a content reference held in an
/// <em>interface-typed</em> member. <see cref="AssetReferenceAnalysis"/> rebuilds the proxy from the
/// value in hand, so it must use the value's concrete type — not the declared member type, which can be
/// an interface with no empty ctor. Reproduces the asset-compile crash
/// "Type &lt;interface&gt; has no empty ctor" seen on a member like <c>IBlockPriceTable PriceTable</c>.
/// </summary>
public class TestContentReferenceInterfaceMember
{
    public interface IMyContent { }

    [DataContract]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContentType>), Profile = "Content")]
    public class MyContentType : IMyContent
    {
        public int Var;
    }

    // Registers MyContentType as a content type.
    [DataContract]
    [AssetDescription(".sdmyct")]
    [AssetContentType(typeof(MyContentType))]
    public class MyContentAsset : Asset { }

    [DataContract]
    [AssetDescription(".sdmyref")]
    public class MyRefAsset : Asset
    {
        // Declared as the interface, but the value is a concrete MyContentType content reference.
        public IMyContent? InterfaceRef { get; set; }
        public MyContentType? ConcreteRef { get; set; }
        // Collection/dictionary shapes with an interface element type exercise the same rebuild paths.
        [NonIdentifiableCollectionItems] public List<IMyContent> InterfaceList { get; init; } = [];
        [NonIdentifiableCollectionItems] public Dictionary<string, IMyContent> InterfaceDict { get; init; } = [];
    }

    [Fact]
    public void CleanResolvesInterfaceTypedContentReference()
    {
        var baseId = AssetId.New();

        // Ten colliding copies (same id/location) force AssetCollision.Clean to remap ids and re-run
        // the reference setters. The references point at the colliding id so they get updated, which is
        // where AssetReferenceAnalysis rebuilds each content-reference proxy (line 307).
        var inputs = new List<AssetItem>();
        for (int i = 0; i < 10; i++)
        {
            var asset = new MyRefAsset
            {
                Id = baseId,
                ConcreteRef = AttachedReferenceManager.CreateProxyObject<MyContentType>(baseId, "bad"),
                InterfaceRef = AttachedReferenceManager.CreateProxyObject<MyContentType>(baseId, "bad"),
            };
            asset.InterfaceList.Add(AttachedReferenceManager.CreateProxyObject<MyContentType>(baseId, "bad"));
            asset.InterfaceDict.Add("k", AttachedReferenceManager.CreateProxyObject<MyContentType>(baseId, "bad"));
            inputs.Add(new AssetItem("0", asset));
        }

        var outputs = new List<AssetItem>();

        // Must not throw "Type ...IMyContent has no empty ctor".
        AssetCollision.Clean(null, inputs, outputs, new AssetResolver(), true, false);

        Assert.Equal(inputs.Count, outputs.Count);

        // The interface-typed reference must survive as a concrete MyContentType proxy (same as the
        // concrete-typed reference), not be dropped or mistyped.
        var cleaned = (MyRefAsset)outputs[0].Asset;
        Assert.IsType<MyContentType>(cleaned.ConcreteRef);
        Assert.IsType<MyContentType>(cleaned.InterfaceRef);
        Assert.IsType<MyContentType>(Assert.Single(cleaned.InterfaceList));
        Assert.IsType<MyContentType>(cleaned.InterfaceDict["k"]);
    }
}
