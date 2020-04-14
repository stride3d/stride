// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;

using Xunit;

using Stride.Shaders.Parser.Mixins;

namespace Stride.Shaders.Tests
{
    /// <summary>
    /// Code used to regenerate all cs files from sdsl/sdfx in the project
    /// </summary>
    public class TestCodeGen
    {
        //[Fact]
        public void Test()
        {
            var filePath = @"D:\Code\Stride\sources\engine\Stride.Shaders.Tests\GameAssets\Mixins\A.sdsl";
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
            //foreach (var sdsl in Directory.EnumerateFiles(directory, "*.sdsl", SearchOption.AllDirectories))
            //{
            //    RebuildFile(sdsl);
            //}
            foreach (var sdfx in Directory.EnumerateFiles(directory, "*.sdfx", SearchOption.AllDirectories))
            {
                RebuildFile(sdfx);
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
