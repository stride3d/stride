using System;
using System.Collections.Generic;
using Xunit;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Tests.Compilers
{
    public class TestDependencyByIncludeTypeAnalysis : CompilerTestBase
    {
        [Fact]
        public void CompilerDependencyByIncludeTypeAnalysis()
        {
            var package = new Package();
            // ReSharper disable once UnusedVariable - we need a package session to compile
            var packageSession = new PackageSession(package);
            var asset1 = new AssetItem("content1", new MyAsset1(), package); // Should be compiled (root)
            var asset2 = new AssetItem("content2", new MyAsset2(), package); // Should be compiled (Runtime for Asset1)
            var asset31 = new AssetItem("content3_1", new MyAsset3(), package); // Should NOT be compiled (CompileAsset for Asset1)
            var asset32 = new AssetItem("content3_2", new MyAsset3(), package); // Should be compiled (Runtime for Asset2)

            ((MyAsset1)asset1.Asset).MyContent2 = AttachedReferenceManager.CreateProxyObject<MyContent2>(asset2.Id, asset2.Location);
            ((MyAsset1)asset1.Asset).MyContent3 = AttachedReferenceManager.CreateProxyObject<MyContent3>(asset31.Id, asset31.Location);
            ((MyAsset2)asset2.Asset).MyContent3 = AttachedReferenceManager.CreateProxyObject<MyContent3>(asset32.Id, asset32.Location);

            package.Assets.Add(asset1);
            package.Assets.Add(asset2);
            package.Assets.Add(asset31);
            package.Assets.Add(asset32);
            package.RootAssets.Add(new AssetReference(asset1.Id, asset1.Location));

            // Create context
            var context = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };

            // Builds the project
            var assetBuilder = new PackageCompiler(new RootPackageAssetEnumerator(package));
            context.Properties.Set(BuildAssetNode.VisitRuntimeTypes, true);
            var assetBuildResult = assetBuilder.Prepare(context);
            // Total number of asset to compile = 3
            Assert.Equal(3, assetBuildResult.BuildSteps.Count);
        }

        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent1>), Profile = "Content")]
        [ContentSerializer(typeof(DataContentSerializer<MyContent1>))]
        public class MyContent1 { }

        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent2>), Profile = "Content")]
        [ContentSerializer(typeof(DataContentSerializer<MyContent2>))]
        public class MyContent2 { }

        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent3>), Profile = "Content")]
        [ContentSerializer(typeof(DataContentSerializer<MyContent3>))]
        public class MyContent3 { }

        [DataContract]
        [AssetDescription(".sdmytest")]
        [AssetContentType(typeof(MyContent1))]
        public class MyAsset1 : Asset
        {
            public MyContent2 MyContent2 { get; set; }
            public MyContent3 MyContent3 { get; set; }
        }

        [DataContract]
        [AssetDescription(".sdmytest")]
        [AssetContentType(typeof(MyContent2))]
        public class MyAsset2 : Asset
        {
            public MyContent3 MyContent3 { get; set; }
        }

        [DataContract]
        [AssetDescription(".sdmytest")]
        [AssetContentType(typeof(MyContent3))]
        public class MyAsset3 : Asset { }

        [AssetCompiler(typeof(MyAsset1), typeof(AssetCompilationContext))]
        public class MyAsset1Compiler : TestAssertCompiler<MyAsset1>
        {
            public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
            {
                yield return new BuildDependencyInfo(typeof(MyAsset2), typeof(AssetCompilationContext), BuildDependencyType.Runtime);
                yield return new BuildDependencyInfo(typeof(MyAsset3), typeof(AssetCompilationContext), BuildDependencyType.CompileAsset);
            }
        }

        [AssetCompiler(typeof(MyAsset2), typeof(AssetCompilationContext))]
        public class MyAsset2Compiler : TestAssertCompiler<MyAsset2>
        {
            public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
            {
                yield return new BuildDependencyInfo(typeof(MyAsset3), typeof(AssetCompilationContext), BuildDependencyType.Runtime);
            }
        }

        [AssetCompiler(typeof(MyAsset3), typeof(AssetCompilationContext))]
        public class MyAsset3Compiler : TestAssertCompiler<MyAsset3> { }
    }
}