using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Tests.Compilers;

public class TestCompilerVisitRuntimeType : CompilerTestBase
{
    [Fact]
    public void CompilerVisitRuntimeType()
    {
        PackageSessionPublicHelper.FindAndSetMSBuildVersion();

        var package = new Package();
        // ReSharper disable once UnusedVariable - we need a package session to compile
        var packageSession = new PackageSession(package);
        var otherAssets = new List<AssetItem>
        {
            new("contentRB", new MyAssetContentType(0), package),
            new("contentRA", new MyAssetContentType(1), package),
            new("content0B", new MyAssetContentType(2), package),
            new("content0M", new MyAssetContentType(3), package),
            new("content0A", new MyAssetContentType(4), package),
            new("content1B", new MyAssetContentType(5), package),
            new("content1M", new MyAssetContentType(6), package),
            new("content1A", new MyAssetContentType(7), package),
            new("content2B", new MyAssetContentType(8), package),
            new("content2M", new MyAssetContentType(9), package),
            new("content2A", new MyAssetContentType(10), package),
            new("content3B", new MyAssetContentType(11), package),
            new("content3M", new MyAssetContentType(12), package),
            new("content3A", new MyAssetContentType(13), package),
            new("content4B", new MyAssetContentType(14), package),
            new("content4M", new MyAssetContentType(15), package),
            new("content4A", new MyAssetContentType(16), package),
        };

        var assetToVisit = new MyAsset1
        {
            Before = AttachedReferenceManager.CreateProxyObject<MyContentType>(otherAssets[0].Id, otherAssets[0].Location),
            Zafter = AttachedReferenceManager.CreateProxyObject<MyContentType>(otherAssets[1].Id, otherAssets[1].Location)
        };
        assetToVisit.RuntimeTypes.Add(CreateRuntimeType(otherAssets[2], otherAssets[3], otherAssets[4]));
        assetToVisit.RuntimeTypes.Add(CreateRuntimeType(otherAssets[5], otherAssets[6], otherAssets[7]));
        assetToVisit.RuntimeTypes.Add(CreateRuntimeType(otherAssets[8], otherAssets[9], otherAssets[10]));
        assetToVisit.RuntimeTypes.Add(CreateRuntimeType(otherAssets[11], otherAssets[12], otherAssets[13]));
        assetToVisit.RuntimeTypes.Add(CreateRuntimeType(otherAssets[14], otherAssets[15], otherAssets[16]));
        assetToVisit.RuntimeTypes[0].A = assetToVisit.RuntimeTypes[1];
        assetToVisit.RuntimeTypes[0].B = assetToVisit.RuntimeTypes[2];
        assetToVisit.RuntimeTypes[1].A = assetToVisit.RuntimeTypes[3];
        assetToVisit.RuntimeTypes[1].B = assetToVisit.RuntimeTypes[4];

        otherAssets.ForEach(x => package.Assets.Add(x));
        var assetItem = new AssetItem("asset", assetToVisit, package);
        package.Assets.Add(assetItem);
        package.RootAssets.Add(new AssetReference(assetItem.Id, assetItem.Location));

        // Create context
        var context = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };

        // Builds the project
        var assetBuilder = new PackageCompiler(new RootPackageAssetEnumerator(package));
        context.Properties.Set(BuildAssetNode.VisitRuntimeTypes, true);
        var assetBuildResult = assetBuilder.Prepare(context);
        Assert.Equal(16, assetBuildResult.BuildSteps.Count);
    }

    private static MyRuntimeType CreateRuntimeType(AssetItem beforeReference, AssetItem middleReference, AssetItem afterReference)
    {
        var result = new MyRuntimeType
        {
            Before = CreateRef<MyContentType>(beforeReference),
            Middle = CreateRef<MyContentType>(middleReference),
            Zafter = CreateRef<MyContentType>(afterReference),
        };
        return result;
    }

    [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContentType>), Profile = "Content")]
    public class MyContentType
    {
        public int Var;
    }

    [DataContract]
    public class MyRuntimeType
    {
        public MyContentType? Before;
        public MyRuntimeType? A;
        public MyContentType? Middle;
        public MyRuntimeType? B;
        public MyContentType? Zafter;
    }

    [DataContract]
    [AssetDescription(FileExtension)]
    [AssetContentType(typeof(MyContentType))]
    public class MyAssetContentType : Asset
    {
        public const string FileExtension = ".sdmact";
        public int Var;
        public MyAssetContentType(int i) { Var = i; }
        public MyAssetContentType() { }
    }

    [DataContract]
    [AssetDescription(".sdmytest")]
    public class MyAsset1 : Asset
    {
        public MyContentType? Before;
        public List<MyRuntimeType> RuntimeTypes = [];
        public MyContentType? Zafter;
    }

    [AssetCompiler(typeof(MyAsset1), typeof(AssetCompilationContext))]
    public class MyAsset1Compiler : TestAssertCompiler<MyAsset1>
    {
        public override IEnumerable<Type> GetRuntimeTypes(AssetItem assetItem)
        {
            yield return typeof(MyRuntimeType);
        }
    }

    [AssetCompiler(typeof(MyAssetContentType), typeof(AssetCompilationContext))]
    public class MyAssetContentTypeCompiler : TestAssertCompiler<MyAssetContentType> { }
}
