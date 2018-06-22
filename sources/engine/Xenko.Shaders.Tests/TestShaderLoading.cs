// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;

using Xunit;

using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Storage;
using Xenko.Games;
using Xenko.Shaders.Parser.Mixins;

using LoggerResult = Xenko.Core.Shaders.Utility.LoggerResult;

namespace Xenko.Shaders.Tests
{
    public class TestShaderLoading
    {
        private ShaderSourceManager sourceManager;
        private ShaderLoader shaderLoader;

        public TestShaderLoading()
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();
            var databaseFileProvider = new DatabaseFileProvider(objDatabase);
            ContentManager.GetFileProvider = () => databaseFileProvider;

            sourceManager = new ShaderSourceManager();
            sourceManager.LookupDirectoryList.Add(@"shaders");
            shaderLoader = new ShaderLoader(sourceManager);
        }

        [Fact(Skip = "This test fixture is unmaintained and currently doesn't pass")]
        public void TestSimple()
        {
            var simple = sourceManager.LoadShaderSource("Simple");

            // Make sure that SourceManager will fail if type is not found
            Assert.Throws<FileNotFoundException>(() => sourceManager.LoadShaderSource("BiduleNotFound"));

            // Reload it and check that it is not loaded twice
            var simple2 = sourceManager.LoadShaderSource("Simple");

            //TODO: cannot compare structure references
            //Assert.That(ReferenceEquals(simple, simple2), Is.True);
            Assert.Equal(simple, simple2);
        }

        [Fact(Skip = "This test fixture is unmaintained and currently doesn't pass")]
        public void TestLoadAst()
        {
            var log = new LoggerResult();

            var simple = shaderLoader.LoadClassSource(new ShaderClassSource("Simple"), new Xenko.Core.Shaders.Parser.ShaderMacro[0], log, false)?.Type;

            Assert.Equal(1, simple.Members.Count);

            var simple2 = shaderLoader.LoadClassSource(new ShaderClassSource("Simple"), new Xenko.Core.Shaders.Parser.ShaderMacro[0], log, false)?.Type;

            // Make sure that a class is not duplicated in memory
            Assert.True(ReferenceEquals(simple, simple2));
        }
    }
}
