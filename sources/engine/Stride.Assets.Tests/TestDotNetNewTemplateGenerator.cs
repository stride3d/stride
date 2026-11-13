// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable
using Stride.Assets.Templates;
using Stride.Core;
using Xunit;

namespace Stride.Assets.Tests
{
    public class TestDotNetNewTemplateGenerator
    {
        [Theory]
        [InlineData("""<PackageReference Include="Stride.Engine" Version="4.4.0" />""", "4.4.0")]
        [InlineData("""<PackageReference Include="Stride.Engine" PrivateAssets="contentfiles;analyzers" Version="4.4.0" />""", "4.4.0")]
        [InlineData("""<PackageReference Include="Stride.Engine"><Version>4.4.0-beta7</Version></PackageReference>""", "4.4.0-beta7")]
        [InlineData("""<PackageReference Include="Stride.Graphics" Version="4.4.0" />""", null)]
        public void ContentEngineVersion(string reference, string? expected)
        {
            var directory = Directory.CreateTempSubdirectory("stride-template-tests").FullName;
            try
            {
                var project = Path.Combine(directory, "Game.csproj");
                File.WriteAllText(project, $"""<Project><ItemGroup>{reference}</ItemGroup></Project>""");

                Assert.Equal(expected is null ? null : new PackageVersion(expected), DotNetNewTemplateGenerator.GetContentEngineVersion([project]));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
