// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Games;
using Stride.Graphics;
using Stride.Shaders.Compiler;
using Stride.Shaders.Parser.Mixins;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;

namespace Stride.Shaders.Tests
{
    public class TestGenericClass
    {
        private ShaderSourceManager manager;
        private Stride.Core.Shaders.Utility.LoggerResult logger;
        private ShaderLoader loader;

        private void Init()
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();
            var databaseFileProvider = new DatabaseFileProvider(objDatabase);

            manager = new ShaderSourceManager(databaseFileProvider);
            manager.LookupDirectoryList.Add("shaders");
            logger = new Stride.Core.Shaders.Utility.LoggerResult();
            loader = new ShaderLoader(manager);
        }

        [Fact(Skip = "This test fixture is unmaintained and currently doesn't pass")]
        public void TestParsing()
        {
            Init();

            var generics = new object[9];
            generics[0] = "Texturing.Texture1";
            generics[1] = "Texturing.Sampler1";
            generics[2] = "TEXCOORD0";
            generics[3] = "CustomLink";
            generics[4] = "1.2f";
            generics[5] = "int2(1,2)";
            generics[6] = "uint3(0,1,2)";
            generics[7] = "float4(5,4,3,2)";
            generics[8] = "StaticClass.staticFloat";
            var shaderClass = loader.LoadClassSource(new ShaderClassSource("GenericClass", generics), null, logger, false)?.Type;

            Assert.NotNull(shaderClass);

            Assert.Equal(10, shaderClass.Members.Count);
            Assert.Equal(4, shaderClass.Members.OfType<Variable>().Count(x => x.Qualifiers.Contains(Stride.Core.Shaders.Ast.Hlsl.StorageQualifier.Static)));
            Assert.Empty(shaderClass.ShaderGenerics);
            Assert.Empty(shaderClass.GenericArguments);
            Assert.Empty(shaderClass.GenericParameters);
            Assert.Single(shaderClass.BaseClasses);

            var linkVar = shaderClass.Members[0] as Variable;
            Assert.NotNull(linkVar);
            var linkName = linkVar.Attributes.OfType<AttributeDeclaration>().Where(x => x.Name.Text == "Link").Select(x => x.Parameters[0].Text).FirstOrDefault();
            Assert.Equal("GenericLink.CustomLink", linkName);

            var baseClass = shaderClass.BaseClasses[0].Name as IdentifierGeneric;
            Assert.NotNull(baseClass);
            Assert.Equal(3, baseClass.Identifiers.Count);
            Assert.Equal("TEXCOORD0", baseClass.Identifiers[0].Text);
            Assert.Equal("CustomLink", baseClass.Identifiers[1].Text);
            Assert.Equal("float4(5,4,3,2)", baseClass.Identifiers[2].Text);
        }

        [Fact(Skip = "This test fixture is unmaintained and currently doesn't pass")]
        public void TestShaderCompilation()
        {
            Init();

            var generics = new string[3];
            generics[0] = "Texturing.Texture1";
            generics[1] = "TEXCOORD0";
            generics[2] = "float4(2.0,1,1,1)";

            var compilerParameters = new CompilerParameters();
            compilerParameters.Set(EffectSourceCodeKeys.Enable, true);
            compilerParameters.EffectParameters.Profile = GraphicsProfile.Level_11_0;

            var mixinSource = new ShaderMixinSource { Name = "TestShaderCompilationGenericClass" };
            mixinSource.Mixins.Add(new ShaderClassSource("GenericClass2", generics));

            var log = new CompilerResults();

            var compiler = new EffectCompiler(TestHelper.CreateDatabaseProvider().FileProvider);
            compiler.SourceDirectories.Add("shaders");

            var effectByteCode = compiler.Compile(mixinSource, compilerParameters.EffectParameters, compilerParameters);
        }


        private void Run()
        {
            TestParsing();
            //TestShaderCompilation();
        }

        private static void Main5()
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();
            var assetIndexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            var databaseFileProvider = new DatabaseFileProvider(assetIndexMap, objDatabase);

            var test = new TestGenericClass();
            test.Run();
        }
    }
}
