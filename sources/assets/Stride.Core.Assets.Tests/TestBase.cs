// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Stride.Core.Assets.Tests
{
    public class TestBase
    {
        // Trailing separator is intentional: callers concatenate subpaths with `+ "subdir\..."`.
        public readonly string DirectoryTestBase = Path.Combine(AssemblyDirectory, "data") + Path.DirectorySeparatorChar;

        public static void GenerateAndCompare(string title, string outputFilePath, string referenceFilePath, Asset asset)
        {
            Console.WriteLine(title + @"- from file " + outputFilePath);
            Console.WriteLine(@"---------------------------------------");
            AssetFileSerializer.Save(outputFilePath, asset, null);
            var left = File.ReadAllText(outputFilePath).Trim();
            Console.WriteLine(left);
            var right = File.ReadAllText(referenceFilePath).Trim();
            Assert.Equal(right, left);
        }

        public static string AssemblyDirectory
            => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    }
}
