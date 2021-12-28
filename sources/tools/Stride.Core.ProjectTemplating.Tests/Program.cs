// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Xunit;

namespace Stride.Core.ProjectTemplating.Tests
{
    public class Program
    {
        [Fact]
        public void TestProjectTemplate()
        {
            var projectTemplate = ProjectTemplate.Load(@"..\..\Test\TestProjectTemplate.ttproj");
            var outputDir = Environment.CurrentDirectory + "\\OutputTemp";
            try
            {
                Directory.Delete(outputDir, true);
            }
            catch (Exception)
            {
            }

            var result = projectTemplate.Generate(outputDir, "TestProject", Guid.NewGuid());
            Assert.False(result.HasErrors);

            Assert.True(File.Exists(Path.Combine(outputDir, "TestProject.cs")));
            Assert.True(File.Exists(Path.Combine(outputDir, @"SubFolder\TextRaw.txt")));
            Assert.True(File.Exists(Path.Combine(outputDir, @"SubFolder\TextTemplate1.cs")));

            Assert.Equal("This is a test with the file name using the property $ProjectName$ = \"TestProject\"", File.ReadAllText(Path.Combine(outputDir, @"TestProject.cs")).Trim());
            Assert.Equal(File.ReadAllText(@"..\..\Test\SubFolder\TextRaw.txt"), File.ReadAllText(Path.Combine(outputDir, @"SubFolder\TextRaw.txt")));
            Assert.Equal("This is a test of template with the project name TestProject and 5", File.ReadAllText(Path.Combine(outputDir, @"SubFolder\TextTemplate1.cs")).Trim());
        }

        static void Main(string[] args)
        {
            var program = new Program();
            program.TestProjectTemplate();
        }
    }
}
