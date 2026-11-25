// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The 4.4 project upgrade: the asset compiler reference is renamed Stride.AssetCompiler, and its assets flow to the executable.
    /// </summary>
    public class TestAssetCompilerReferenceUpgrade : IDisposable
    {
        private readonly string directory = Directory.CreateTempSubdirectory("stride-compiler-upgrade-").FullName;

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void RenamesTheReferenceAndChangesNothingWhenItRunsAgain()
        {
            var path = Path.Combine(directory, "Game.csproj");
            File.WriteAllText(path, """
                <Project>
                  <ItemGroup>
                    <PackageReference Include="Stride.Core.Assets.CompilerApp" Version="4.3.0" IncludeAssets="build" PrivateAssets="all" />
                  </ItemGroup>
                </Project>
                """);

            var context = Upgrade(path);
            Assert.True(context.IsDirty);
            context.Project.Save();
            var reference = context.Project.Xml.Items.Single(x => x.ItemType == "PackageReference");
            Assert.Equal("Stride.AssetCompiler", reference.Include);
            Assert.Equal("build;buildTransitive", reference.Metadata.Single(x => x.Name == "IncludeAssets").Value);
            Assert.DoesNotContain(reference.Metadata, x => x.Name == "PrivateAssets");

            // As a retry of the step does
            Assert.False(Upgrade(path).IsDirty);
        }

        private static ProjectUpgradeContext Upgrade(string path)
        {
            var project = new Project(path, null, null, new Microsoft.Build.Evaluation.ProjectCollection());
            var context = new ProjectUpgradeContext(project, null!, new UFile(path), new LoggerResult());
            StridePackageUpgrader.UpgradeProjectTo44(context);
            return context;
        }
    }
}
