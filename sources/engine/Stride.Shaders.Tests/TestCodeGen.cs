// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;

using Xunit;

using Xenko.Shaders.Parser.Mixins;

namespace Xenko.Shaders.Tests
{
    /// <summary>
    /// Code used to regenerate all cs files from xksl/xkfx in the project
    /// </summary>
    public class TestCodeGen
    {
        //[Fact]
        public void Test()
        {
            var filePath = @"D:\Code\Xenko\sources\engine\Xenko.Shaders.Tests\GameAssets\Mixins\A.xksl";
            var source = File.ReadAllText(filePath);
            var content = ShaderMixinCodeGen.GenerateCsharp(source, filePath.Replace("C:", "D:"));
        }

        //[Fact] // Decomment this line to regenerate all files (sources and samples)
        public void RebuildAllXkfxXksl()
        {
            RegenerateDirectory(Path.Combine(Environment.CurrentDirectory, @"..\..\sources"));
            RegenerateDirectory(Path.Combine(Environment.CurrentDirectory, @"..\..\samples"));
        }

        private static void RegenerateDirectory(string directory)
        {
            //foreach (var xksl in Directory.EnumerateFiles(directory, "*.xksl", SearchOption.AllDirectories))
            //{
            //    RebuildFile(xksl);
            //}
            foreach (var xkfx in Directory.EnumerateFiles(directory, "*.xkfx", SearchOption.AllDirectories))
            {
                RebuildFile(xkfx);
            }
        }

        private static void RebuildFile(string filePath)
        {
            try
            {
                var source = File.ReadAllText(filePath);
                var content = ShaderMixinCodeGen.GenerateCsharp(source, filePath);

                // Sometimes, we have a collision with the .cs file, so generated filename might be postfixed with 1
                var destPath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "1.cs");
                if (!File.Exists(destPath))
                    destPath = Path.ChangeExtension(filePath, ".cs");
                if (!File.Exists(destPath))
                {
                    Console.WriteLine("Target file {0} doesn't exist", destPath);
                    return;
                }
                File.WriteAllText(destPath, content, Encoding.UTF8);
                Console.WriteLine("File generated {0}", filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error {0}: {1}", filePath, ex);
            }
        }
    }
}
