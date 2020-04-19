using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Tests.Compilers
{
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
                new AssetItem("contentRB", new MyAssetContentType(0), package),
                new AssetItem("contentRA", new MyAssetContentType(1), package),
                new AssetItem("content0B", new MyAssetContentType(2), package),
                new AssetItem("content0M", new MyAssetContentType(3), package),
                new AssetItem("content0A", new MyAssetContentType(4), package),
                new AssetItem("content1B", new MyAssetContentType(5), package),
                new AssetItem("content1M", new MyAssetContentType(6), package),
                new AssetItem("content1A", new MyAssetContentType(7), package),
                new AssetItem("content2B", new MyAssetContentType(8), package),
                new AssetItem("content2M", new MyAssetContentType(9), package),
                new AssetItem("content2A", new MyAssetContentType(10), package),
                new AssetItem("content3B", new MyAssetContentType(11), package),
                new AssetItem("content3M", new MyAssetContentType(12), package),
                new AssetItem("content3A", new MyAssetContentType(13), package),
                new AssetItem("content4B", new MyAssetContentType(14), package),
                new AssetItem("content4M", new MyAssetContentType(15), package),
                new AssetItem("content4A", new MyAssetContentType(16), package),
            };

            var assetToVisit = new MyAsset1();
            assetToVisit.Before = AttachedReferenceManager.CreateProxyObject<MyContentType>(otherAssets[0].Id, otherAssets[0].Location);
            assetToVisit.Zafter = AttachedReferenceManager.CreateProxyObject<MyContentType>(otherAssets[1].Id, otherAssets[1].Location);
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
            public MyContentType Before;
            public MyRuntimeType A;
            public MyContentType Middle;
            public MyRuntimeType B;
            public MyContentType Zafter;
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
            public MyContentType Before;
            public List<MyRuntimeType> RuntimeTypes = new List<MyRuntimeType>();
            public MyContentType Zafter;
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
}
