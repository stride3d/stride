using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Tests.Compilers
{
    public class TestBuildDependencyManager : CompilerTestBase
    {
        [Fact]
        public void TestCompileAsset()
        {
            var package = new Package();
            // ReSharper disable once UnusedVariable - we need a package session to compile
            var packageSession = new PackageSession(package);
            var otherAssets = new List<AssetItem>
            {
                new AssetItem("asset5", new MyAsset5(), package),
                new AssetItem("asset6", new MyAsset6(), package),
                new AssetItem("asset7", new MyAsset7(), package),
            };
            otherAssets.ForEach(x => package.Assets.Add(x));

            var compileAssetReference = new MyAsset2
            {
                CompileAssetReference = CreateRef<MyContent5>(otherAssets[0]),
                CompileContentReference = CreateRef<MyContent6>(otherAssets[1]),
                CompileRuntimeReference = CreateRef<MyContent7>(otherAssets[2]),
            };
            var assetItem = new AssetItem("asset2", compileAssetReference, package);
            package.Assets.Add(assetItem);

            var asset = new MyAsset1 { CompileAssetReference = CreateRef<MyContent2>(assetItem) };
            assetItem = new AssetItem("asset1", asset, package);
            package.Assets.Add(assetItem);
            package.RootAssets.Add(new AssetReference(assetItem.Id, assetItem.Location));

            // Create context
            var context = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };
            // Builds the project
            Exception ex = null;
            MyAsset1Compiler.AssertFunc = (url, ass, pkg) =>
            {
                // Nothing must have been compiled before
                AssertInThread(ref ex, () => Assert.Empty(TestCompilerBase.CompiledAssets));
            };

            var assetBuilder = new PackageCompiler(new RootPackageAssetEnumerator(package));
            var assetBuildResult = assetBuilder.Prepare(context);
            // Since MyAsset2 is a CompileAsset reference, it should not be compiled, so we should have only 1 asset (MyAsset1) to compile.
            Assert.Equal(1, assetBuildResult.BuildSteps.Count);
            var builder = new Builder(GlobalLogger.GetLogger("Test"), "", "");
            builder.Root.Add(assetBuildResult.BuildSteps);
            builder.Run(Builder.Mode.Build, false);
            RethrowAssertsFromThread(ex);
        }

        [Fact]
        public void TestCompileContent()
        {
            var package = new Package();
            // ReSharper disable once UnusedVariable - we need a package session to compile
            var packageSession = new PackageSession(package);
            var otherAssets = new List<AssetItem>
            {
                new AssetItem("asset8", new MyAsset8(), package),
                new AssetItem("asset9", new MyAsset9(), package),
                new AssetItem("asset10", new MyAsset10(), package),
            };
            otherAssets.ForEach(x => package.Assets.Add(x));

            var compileAssetReference = new MyAsset3
            {
                CompileAssetReference = CreateRef<MyContent8>(otherAssets[0]),
                CompileContentReference = CreateRef<MyContent9>(otherAssets[1]),
                CompileRuntimeReference = CreateRef<MyContent10>(otherAssets[2]),
            };
            var asset3 = new AssetItem("asset3", compileAssetReference, package);
            package.Assets.Add(asset3);

            var asset = new MyAsset1 { CompileContentReference = CreateRef<MyContent3>(asset3) };
            var assetItem = new AssetItem("asset1", asset, package);
            package.Assets.Add(assetItem);
            package.RootAssets.Add(new AssetReference(assetItem.Id, assetItem.Location));

            // Create context
            var context = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };

            Exception ex = null;
            MyAsset1Compiler.AssertFunc = (url, ass, pkg) =>
            {
                AssertInThread(ref ex, () => Assert.Single(TestCompilerBase.CompiledAssets));
                AssertInThread(ref ex, () => Assert.Equal(asset3.Id, TestCompilerBase.CompiledAssets.First().Id));
            };

            var assetBuilder = new PackageCompiler(new RootPackageAssetEnumerator(package));
            var assetBuildResult = assetBuilder.Prepare(context);
            // Since MyAsset3 is a CompileContent reference, it should be compiled, so we should have only 2 asset (MyAsset1 and MyAsset3) to compile.
            Assert.Equal(2, assetBuildResult.BuildSteps.Count);
            var builder = new Builder(GlobalLogger.GetLogger("Test"), "", "");
            builder.Root.Add(assetBuildResult.BuildSteps);
            builder.Run(Builder.Mode.Build, false);
            RethrowAssertsFromThread(ex);
        }

        [Fact]
        public void TestRuntime()
        {
            var package = new Package();
            // ReSharper disable once UnusedVariable - we need a package session to compile
            var packageSession = new PackageSession(package);
            var otherAssets = new List<AssetItem>
            {
                new AssetItem("asset11", new MyAsset11(), package),
                new AssetItem("asset12", new MyAsset12(), package),
                new AssetItem("asset13", new MyAsset13(), package),
            };
            otherAssets.ForEach(x => package.Assets.Add(x));

            var compileAssetReference = new MyAsset4
            {
                CompileAssetReference = CreateRef<MyContent11>(otherAssets[0]),
                CompileContentReference = CreateRef<MyContent12>(otherAssets[1]),
                CompileRuntimeReference = CreateRef<MyContent13>(otherAssets[2]),
            };
            var assetItem = new AssetItem("asset4", compileAssetReference, package);
            package.Assets.Add(assetItem);

            var asset = new MyAsset1 { CompileAssetReference = CreateRef<MyContent2>(assetItem) };
            assetItem = new AssetItem("asset1", asset, package);
            package.Assets.Add(assetItem);
            package.RootAssets.Add(new AssetReference(assetItem.Id, assetItem.Location));

            // Create context
            var context = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };

            // Builds the project
            Exception ex = null;
            MyAsset1Compiler.AssertFunc = (url, ass, pkg) =>
            {
                AssertInThread(ref ex, () => Assert.Single(TestCompilerBase.CompiledAssets));
                AssertInThread(ref ex, () => Assert.Equal(compileAssetReference.Id, TestCompilerBase.CompiledAssets.First().Id));
            };

            var assetBuilder = new PackageCompiler(new RootPackageAssetEnumerator(package));
            var assetBuildResult = assetBuilder.Prepare(context);
            // Since MyAsset4 is a Runtime reference, it should be compiled, so we should have 2 asset (MyAsset1 and MyAsset4) to compile.
            Assert.Equal(2, assetBuildResult.BuildSteps.Count);
            var builder = new Builder(GlobalLogger.GetLogger("Test"), "", "");
            builder.Root.Add(assetBuildResult.BuildSteps);
            builder.Run(Builder.Mode.Build, false);
            RethrowAssertsFromThread(ex);
        }


        private static void AssertInThread(ref Exception ex, Action assert)
        {
            try
            {
                assert();
            }
            catch (Exception e)
            {
                ex = e;
            }
        }

        private static void RethrowAssertsFromThread(Exception ex)
        {
            if (ex != null)
            {
                throw ex;
            }
        }

        #region Types

        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent1>), Profile = "Content")]
        public class MyContent1 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent2>), Profile = "Content")]
        public class MyContent2 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent3>), Profile = "Content")]
        public class MyContent3 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent4>), Profile = "Content")]
        public class MyContent4 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent5>), Profile = "Content")]
        public class MyContent5 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent6>), Profile = "Content")]
        public class MyContent6 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent7>), Profile = "Content")]
        public class MyContent7 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent8>), Profile = "Content")]
        public class MyContent8 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent9>), Profile = "Content")]
        public class MyContent9 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent10>), Profile = "Content")]
        public class MyContent10 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent11>), Profile = "Content")]
        public class MyContent11 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent12>), Profile = "Content")]
        public class MyContent12 { }
        [DataContract, ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<MyContent13>), Profile = "Content")]
        public class MyContent13 { }

        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent1))]
        public class MyAsset1 : Asset
        {
            public MyContent2 CompileAssetReference;
            public MyContent3 CompileContentReference;
            public MyContent4 CompileRuntimeReference;
        }

        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent2))]
        public class MyAsset2 : Asset
        {
            public MyContent5 CompileAssetReference;
            public MyContent6 CompileContentReference;
            public MyContent7 CompileRuntimeReference;
        }

        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent3))]
        public class MyAsset3 : Asset
        {
            public MyContent8 CompileAssetReference;
            public MyContent9 CompileContentReference;
            public MyContent10 CompileRuntimeReference;
        }

        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent4))]
        public class MyAsset4 : Asset
        {
            public MyContent11 CompileAssetReference;
            public MyContent12 CompileContentReference;
            public MyContent13 CompileRuntimeReference;
        }

        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent5))]
        public class MyAsset5 : Asset { }
        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent6))]
        public class MyAsset6 : Asset { }
        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent7))]
        public class MyAsset7 : Asset { }
        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent8))]
        public class MyAsset8 : Asset { }
        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent9))]
        public class MyAsset9 : Asset { }
        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent10))]
        public class MyAsset10 : Asset { }
        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent11))]
        public class MyAsset11 : Asset { }
        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent12))]
        public class MyAsset12 : Asset { }
        [DataContract, AssetDescription(".sdmytest"), AssetContentType(typeof(MyContent13))]
        public class MyAsset13 : Asset { }

        [AssetCompiler(typeof(MyAsset1), typeof(AssetCompilationContext))]
        public class MyAsset1Compiler : TestAssertCompiler<MyAsset1>
        {
            public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
            {
                yield return new BuildDependencyInfo(typeof(MyAsset2), typeof(AssetCompilationContext), BuildDependencyType.CompileAsset);
                yield return new BuildDependencyInfo(typeof(MyAsset3), typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
                yield return new BuildDependencyInfo(typeof(MyAsset4), typeof(AssetCompilationContext), BuildDependencyType.Runtime);
            }

            public static Action<string, MyAsset1, IAssetFinder> AssertFunc;
            protected override void DoCommandAssert(string url, MyAsset1 parameters, IAssetFinder package) => AssertFunc?.Invoke(url, parameters, package);
        }

        [AssetCompiler(typeof(MyAsset2), typeof(AssetCompilationContext))]
        public class MyAsset2Compiler : TestAssertCompiler<MyAsset2> { }
        [AssetCompiler(typeof(MyAsset3), typeof(AssetCompilationContext))]
        public class MyAsset3Compiler : TestAssertCompiler<MyAsset3> { }
        [AssetCompiler(typeof(MyAsset4), typeof(AssetCompilationContext))]
        public class MyAsset4Compiler : TestAssertCompiler<MyAsset4> { }
        [AssetCompiler(typeof(MyAsset5), typeof(AssetCompilationContext))]
        public class MyAsset5Compiler : TestAssertCompiler<MyAsset5> { }
        [AssetCompiler(typeof(MyAsset6), typeof(AssetCompilationContext))]
        public class MyAsset6Compiler : TestAssertCompiler<MyAsset6> { }
        [AssetCompiler(typeof(MyAsset7), typeof(AssetCompilationContext))]
        public class MyAsset7Compiler : TestAssertCompiler<MyAsset7> { }
        [AssetCompiler(typeof(MyAsset8), typeof(AssetCompilationContext))]
        public class MyAsset8Compiler : TestAssertCompiler<MyAsset8> { }
        [AssetCompiler(typeof(MyAsset9), typeof(AssetCompilationContext))]
        public class MyAsset9Compiler : TestAssertCompiler<MyAsset9> { }
        [AssetCompiler(typeof(MyAsset10), typeof(AssetCompilationContext))]
        public class MyAsset10Compiler : TestAssertCompiler<MyAsset10> { }
        [AssetCompiler(typeof(MyAsset11), typeof(AssetCompilationContext))]
        public class MyAsset11Compiler : TestAssertCompiler<MyAsset11> { }
        [AssetCompiler(typeof(MyAsset12), typeof(AssetCompilationContext))]
        public class MyAsset12Compiler : TestAssertCompiler<MyAsset12> { }
        [AssetCompiler(typeof(MyAsset13), typeof(AssetCompilationContext))]
        public class MyAsset13Compiler : TestAssertCompiler<MyAsset13> { }

        #endregion Types
    }
}
