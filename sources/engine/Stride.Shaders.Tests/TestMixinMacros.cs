// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xunit;

using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Shaders.Parser;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;

namespace Stride.Shaders.Tests
{
    public class TestMixinMacros
    {
        private ShaderMixinParser shaderMixinParser;

        private void Init()
        {
            // Create and mount database file system
            var objDatabase = ObjectDatabase.CreateDefaultDatabase();
            var databaseFileProvider = new DatabaseFileProvider(objDatabase);

            shaderMixinParser = new ShaderMixinParser(databaseFileProvider);
            shaderMixinParser.SourceManager.LookupDirectoryList.Add("/shaders"); 
        }

        [Fact(Skip = "This test fixture is unmaintained and currently doesn't pass")]
        public void TestMacros()
        {
            Init();

            // test that macros are correctly used
            var baseMixin = new ShaderMixinSource();
            baseMixin.AddMacro("STRIDE_GRAPHICS_API_DIRECT3D", 1);
            baseMixin.Macros.Add(new ShaderMacro("MACRO_TEST", "int"));
            baseMixin.Mixins.Add(new ShaderClassSource("TestMacros"));
            
            var macros0 = new ShaderMixinSource();
            macros0.Mixins.Add(new ShaderClassSource("MacroTest"));
            baseMixin.Compositions.Add("macros0", macros0);

            var macros1 = new ShaderMixinSource();
            macros1.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros1.Macros.Add(new ShaderMacro("MACRO_TEST", "float"));
            baseMixin.Compositions.Add("macros1", macros1);

            var macros2 = new ShaderMixinSource();
            macros2.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros2.Macros.Add(new ShaderMacro("MACRO_TEST", "float4"));
            baseMixin.Compositions.Add("macros2", macros2);

            var parsingResult = shaderMixinParser.Parse(baseMixin, baseMixin.Macros.ToArray());
            
            Assert.False(parsingResult.HasErrors);
            var cBufferVar = parsingResult.Shader.Declarations.OfType<ConstantBuffer>().First(x => x.Name == "Globals").Members.OfType<Variable>().ToList();
            Assert.Equal(1, cBufferVar.Count(x => x.Type.Name.Text == "int"));
            Assert.Equal(1, cBufferVar.Count(x => x.Type.Name.Text == "float"));
            Assert.Equal(1, cBufferVar.Count(x => x.Type.Name.Text == "float4"));

            // test clash when reloading
            var baseMixin2 = new ShaderMixinSource();
            baseMixin2.AddMacro("STRIDE_GRAPHICS_API_DIRECT3D", 1);
            baseMixin2.Macros.Add(new ShaderMacro("MACRO_TEST", "int"));
            baseMixin2.Mixins.Add(new ShaderClassSource("TestMacros"));

            var macros3 = new ShaderMixinSource();
            macros3.Mixins.Add(new ShaderClassSource("MacroTest"));
            baseMixin2.Compositions.Add("macros0", macros3);

            var macros4 = new ShaderMixinSource();
            macros4.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros4.Macros.Add(new ShaderMacro("MACRO_TEST", "uint4"));
            baseMixin2.Compositions.Add("macros1", macros4);

            var macros5 = new ShaderMixinSource();
            macros5.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros5.Macros.Add(new ShaderMacro("MACRO_TEST", "float4"));
            baseMixin2.Compositions.Add("macros2", macros5);

            var parsingResult2 = shaderMixinParser.Parse(baseMixin2, baseMixin2.Macros.ToArray());

            Assert.False(parsingResult.HasErrors);
            var cBufferVar2 = parsingResult2.Shader.Declarations.OfType<ConstantBuffer>().First(x => x.Name == "Globals").Members.OfType<Variable>().ToList();
            Assert.Equal(1, cBufferVar2.Count(x => x.Type.Name.Text == "int"));
            Assert.Equal(1, cBufferVar2.Count(x => x.Type.Name.Text == "uint4"));
            Assert.Equal(1, cBufferVar2.Count(x => x.Type.Name.Text == "float4"));
        }

        [Fact(Skip = "This test fixture is unmaintained and currently doesn't pass")]
        public void TestMacrosArray()
        {
            Init();

            // test that macros are correctly used through an array
            var baseMixin = new ShaderMixinSource();
            baseMixin.AddMacro("STRIDE_GRAPHICS_API_DIRECT3D", 1);
            baseMixin.Macros.Add(new ShaderMacro("MACRO_TEST", "int"));
            baseMixin.Mixins.Add(new ShaderClassSource("TestMacrosArray"));

            var compositionArray = new ShaderArraySource();

            var macros0 = new ShaderMixinSource();
            macros0.Mixins.Add(new ShaderClassSource("MacroTest"));
            compositionArray.Add(macros0);

            var macros1 = new ShaderMixinSource();
            macros1.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros1.Macros.Add(new ShaderMacro("MACRO_TEST", "float"));
            compositionArray.Add(macros1);

            var macros2 = new ShaderMixinSource();
            macros2.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros2.Macros.Add(new ShaderMacro("MACRO_TEST", "float4"));
            compositionArray.Add(macros2);
            
            baseMixin.Compositions.Add("macrosArray", compositionArray);

            var parsingResult = shaderMixinParser.Parse(baseMixin, baseMixin.Macros.ToArray());

            Assert.False(parsingResult.HasErrors);
            var cBufferVar = parsingResult.Shader.Declarations.OfType<ConstantBuffer>().First(x => x.Name == "Globals").Members.OfType<Variable>().ToList();
            Assert.Equal(1, cBufferVar.Count(x => x.Type.Name.Text == "int"));
            Assert.Equal(1, cBufferVar.Count(x => x.Type.Name.Text == "float"));
            Assert.Equal(1, cBufferVar.Count(x => x.Type.Name.Text == "float4"));
        }


        private void Run()
        {
            //TestMacros();
            TestMacrosArray();
        }
    }
}
