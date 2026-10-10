// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using Xunit;
using Stride.Core.Assets.Compiler;

namespace Stride.Core.Assets.Tests
{
    public class TestBuildManifestSession : TestBase
    {
        [Fact]
        public void TestPlatformHeadPackageRootAssets()
        {
            // A platform head redirected to the game's sdpkg (StrideCurrentPackagePath) is, as in the
            // editor, a package of its own: the sdpkg next to its csproj, where the editor records
            // "Include in build as root asset" entries, depending on the game's package. Root
            // enumeration from the head must include both packages' roots.
            var dirPath = Path.Combine(DirectoryTestBase, "TestBuildManifestRootAssets");
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
            Directory.CreateDirectory(Path.Combine(dirPath, "MyGame.Game", "obj"));
            Directory.CreateDirectory(Path.Combine(dirPath, "MyGame.Game", "Assets", "UI"));
            Directory.CreateDirectory(Path.Combine(dirPath, "MyGame.Windows", "obj"));

            var gameRootId = AssetId.New();
            var headRootId = AssetId.New();
            var sharedId = AssetId.New();
            var unrootedId = AssetId.New();

            foreach (var (id, name) in new[] { (gameRootId, "GameRoot"), (headRootId, "Subject"), (sharedId, "Shared"), (unrootedId, "Unrooted") })
            {
                File.WriteAllText(Path.Combine(dirPath, "MyGame.Game", "Assets", "UI", name + ".sdtest"),
                    $"""
                    !AssetObjectTest
                    Id: {id}
                    Name: {name}
                    """);
            }

            File.WriteAllText(Path.Combine(dirPath, "MyGame.Game", "MyGame.Game.sdpkg"),
                $$"""
                !Package
                SerializedVersion: {Assets: 3.1.0.0}
                Meta:
                    Name: MyGame
                    Version: 1.0.0
                AssetFolders:
                    -   Path: !dir Assets
                RootAssets:
                    - {{gameRootId}}:UI/GameRoot
                    - {{sharedId}}:UI/Shared
                """);
            File.WriteAllText(Path.Combine(dirPath, "MyGame.Game", "obj", "MyGame.Game.sdbuild"),
                """
                !AssetBuildManifest
                Version: 1
                ProjectFile: "../MyGame.Game.csproj"
                PackageFile: "../MyGame.Game.sdpkg"
                PackageName: "MyGame.Game"
                """);

            // Head package holds one entry of its own and one duplicating the game's
            File.WriteAllText(Path.Combine(dirPath, "MyGame.Windows", "MyGame.Windows.sdpkg"),
                $$"""
                !Package
                SerializedVersion: {Assets: 3.1.0.0}
                Meta:
                    Name: MyGame.Windows
                    Version: 1.0.0
                RootAssets:
                    - {{headRootId}}:UI/Subject
                    - {{sharedId}}:UI/Shared
                """);
            File.WriteAllText(Path.Combine(dirPath, "MyGame.Windows", "obj", "MyGame.Windows.sdbuild"),
                """
                !AssetBuildManifest
                Version: 1
                ProjectFile: "../MyGame.Windows.csproj"
                PackageFile: "../../MyGame.Game/MyGame.Game.sdpkg"
                PackageName: "MyGame.Windows"
                ReferencedManifests:
                    - "../../MyGame.Game/obj/MyGame.Game.sdbuild"
                """);

            var sessionResult = new PackageSessionResult();
            var rootPackage = PackageSession.LoadFromBuildManifest(Path.Combine(dirPath, "MyGame.Windows", "obj", "MyGame.Windows.sdbuild"), sessionResult);
            Assert.False(sessionResult.HasErrors, string.Join(Environment.NewLine, sessionResult.Messages.Select(x => x.ToString())));

            // The head is a package of its own, named after its project, depending on the game's
            var headPackage = sessionResult.Session.Packages.Single(x => x.FullPath is not null && x.FullPath.ToString().EndsWith("MyGame.Windows.sdpkg", StringComparison.OrdinalIgnoreCase));
            var gamePackage = sessionResult.Session.Packages.Single(x => x.FullPath is not null && x.FullPath.ToString().EndsWith("MyGame.Game.sdpkg", StringComparison.OrdinalIgnoreCase));
            Assert.Same(headPackage, rootPackage);
            Assert.Equal("MyGame.Windows", headPackage.Meta.Name);
            Assert.Equal("MyGame.Windows", headPackage.Container.AssetNamespace);
            Assert.Contains(headPackage.Container.FlattenedDependencies, x => x.Package == gamePackage);
            Assert.True(headPackage.RootAssets.ContainsKey(headRootId));
            Assert.Equal(2, headPackage.RootAssets.Count);

            // The authored package keeps exactly its own entries and identity
            Assert.Equal("MyGame.Game", gamePackage.Meta.Name);
            Assert.Equal("MyGame", gamePackage.Container.AssetNamespace);
            Assert.True(gamePackage.RootAssets.ContainsKey(gameRootId));
            Assert.Equal(2, gamePackage.RootAssets.Count);

            // Root enumeration from the head walks both packages' roots, not unrooted assets
            var compilerResult = new AssetCompilerResult();
            var enumerated = new RootPackageAssetEnumerator(headPackage).GetAssets(compilerResult).Select(x => x.Id).ToHashSet();
            Assert.False(compilerResult.HasErrors, string.Join(Environment.NewLine, compilerResult.Messages.Select(x => x.ToString())));
            Assert.Contains(headRootId, enumerated);
            Assert.Contains(gameRootId, enumerated);
            Assert.Contains(sharedId, enumerated);
            Assert.DoesNotContain(unrootedId, enumerated);
        }

        [Fact]
        public void TestDevRedirectDeclarationsComeFromTheProjectManifest()
        {
            // A dev-redirect stub generated from another state of the checkout still declares a companion; the
            // project's own build manifest, written by its last build, declares none
            var dirPath = Path.Combine(DirectoryTestBase, "TestDevRedirectDeclarationsComeFromTheProjectManifest");
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
            var projectDirectory = Path.Combine(dirPath, "DevLib");
            var stubDirectory = Path.Combine(dirPath, "packages", "devlib", "1.0.0");
            Directory.CreateDirectory(Path.Combine(projectDirectory, "obj", "Debug", "net10.0"));
            Directory.CreateDirectory(Path.Combine(stubDirectory, "stride"));
            Directory.CreateDirectory(Path.Combine(stubDirectory, "build"));
            Directory.CreateDirectory(Path.Combine(dirPath, "MyGame", "obj"));

            File.WriteAllText(Path.Combine(projectDirectory, "DevLib.csproj"), "<Project />");
            File.WriteAllText(Path.Combine(projectDirectory, "DevLib.sdpkg"),
                """
                !Package
                SerializedVersion: {Assets: 3.1.0.0}
                Meta:
                    Name: DevLib
                    Version: 1.0.0
                """);
            File.WriteAllText(Path.Combine(projectDirectory, "obj", "Debug", "net10.0", "DevLib.sdbuild"),
                """
                !AssetBuildManifest
                Version: 1
                ProjectFile: "../../../DevLib.csproj"
                PackageFile: "../../../DevLib.sdpkg"
                PackageName: "DevLib"
                """);

            File.WriteAllText(Path.Combine(stubDirectory, "stride", "DevLib.sdpkg"),
                """
                !Package
                SerializedVersion: {Assets: 3.1.0.0}
                Meta:
                    Name: DevLib
                    Version: 1.0.0
                AssetNamespace: DevLib
                CompanionPackages:
                    -   Name: DevLib.Assets
                        Version: 1.0.0
                        Kind: Assets
                """);
            File.WriteAllText(Path.Combine(stubDirectory, "build", "DevLib.props"),
                $"""
                <Project>
                  <ItemGroup Condition="false">
                    <Reference Include="DevLib">
                      <StrideDevProjectDirectory>{projectDirectory}</StrideDevProjectDirectory>
                    </Reference>
                  </ItemGroup>
                </Project>
                """);

            File.WriteAllText(Path.Combine(dirPath, "MyGame", "MyGame.sdpkg"),
                """
                !Package
                SerializedVersion: {Assets: 3.1.0.0}
                Meta:
                    Name: MyGame
                    Version: 1.0.0
                """);
            File.WriteAllText(Path.Combine(dirPath, "MyGame", "obj", "project.assets.json"),
                $$"""
                {
                  "version": 3,
                  "targets": { "net10.0": { "DevLib/1.0.0": { "type": "package" } } },
                  "libraries": { "DevLib/1.0.0": { "type": "package", "path": "devlib/1.0.0", "files": [ "build/DevLib.props", "stride/DevLib.sdpkg" ] } },
                  "projectFileDependencyGroups": { "net10.0": [ "DevLib >= 1.0.0" ] },
                  "packageFolders": { "{{(Path.Combine(dirPath, "packages") + Path.DirectorySeparatorChar).Replace("\\", "\\\\")}}": {} },
                  "project": { "version": "1.0.0", "frameworks": { "net10.0": {} } }
                }
                """);
            File.WriteAllText(Path.Combine(dirPath, "MyGame", "obj", "MyGame.sdbuild"),
                """
                !AssetBuildManifest
                Version: 1
                ProjectFile: "../MyGame.csproj"
                PackageFile: "../MyGame.sdpkg"
                PackageName: "MyGame"
                TargetFramework: "net10.0"
                NuGetLockFile: "project.assets.json"
                """);

            var sessionResult = new PackageSessionResult();
            PackageSession.LoadFromBuildManifest(Path.Combine(dirPath, "MyGame", "obj", "MyGame.sdbuild"), sessionResult);

            // The stale stub's companion would be loaded and reported missing
            Assert.False(sessionResult.HasErrors, string.Join(Environment.NewLine, sessionResult.Messages.Select(x => x.ToString())));
            var devLib = sessionResult.Session.Packages.Single(x => x.Meta.Name == "DevLib");
            Assert.Empty(devLib.CompanionPackages);
            Assert.Equal("DevLib", devLib.AssetNamespace);
        }

        [Fact]
        public void TestManifestReadsOldAssetAssembliesKey()
        {
            // A manifest written before HostAssemblies was named AssetAssemblies
            var dirPath = Path.Combine(DirectoryTestBase, "TestManifestReadsOldAssetAssembliesKey");
            Directory.CreateDirectory(dirPath);
            var manifestFile = Path.Combine(dirPath, "Old.sdbuild");
            File.WriteAllText(manifestFile,
                """
                !AssetBuildManifest
                Version: 1
                PackageName: "Old"
                AssetAssemblies:
                    - "../bin/Old.dll"
                """);

            var manifest = Stride.Core.Yaml.YamlSerializer.Load<AssetBuildManifest>(manifestFile);
            Assert.Equal("../bin/Old.dll", Assert.Single(manifest.HostAssemblies).ToString());
        }

        [Fact]
        public void TestRedirectedPackageWithoutOwningProject()
        {
            // A project redirected to a sdpkg no project sits next to owns that package: it is the
            // project's own package, not a dependency, and no implicit package appears next to the csproj.
            var dirPath = Path.Combine(DirectoryTestBase, "TestBuildManifestRedirectedPackage");
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
            Directory.CreateDirectory(Path.Combine(dirPath, "MyGame.Game", "obj"));
            Directory.CreateDirectory(Path.Combine(dirPath, "Assets"));

            var rootId = AssetId.New();
            File.WriteAllText(Path.Combine(dirPath, "Assets", "Root.sdtest"),
                $"""
                !AssetObjectTest
                Id: {rootId}
                Name: Root
                """);
            File.WriteAllText(Path.Combine(dirPath, "MyGame.sdpkg"),
                $$"""
                !Package
                SerializedVersion: {Assets: 3.1.0.0}
                Meta:
                    Name: MyGame
                    Version: 1.0.0
                AssetFolders:
                    -   Path: !dir Assets
                RootAssets:
                    - {{rootId}}:Root
                """);
            File.WriteAllText(Path.Combine(dirPath, "MyGame.Game", "obj", "MyGame.Game.sdbuild"),
                """
                !AssetBuildManifest
                Version: 1
                ProjectFile: "../MyGame.Game.csproj"
                PackageFile: "../../MyGame.sdpkg"
                PackageName: "MyGame.Game"
                """);

            var sessionResult = new PackageSessionResult();
            var package = PackageSession.LoadFromBuildManifest(Path.Combine(dirPath, "MyGame.Game", "obj", "MyGame.Game.sdbuild"), sessionResult);
            Assert.False(sessionResult.HasErrors, string.Join(Environment.NewLine, sessionResult.Messages.Select(x => x.ToString())));

            Assert.Same(package, Assert.Single(sessionResult.Session.Packages));
            Assert.Equal(Path.Combine(dirPath, "MyGame.sdpkg"), package.FullPath.ToOSPath(), ignoreCase: true);
            Assert.True(package.RootAssets.ContainsKey(rootId));
        }
    }
}
