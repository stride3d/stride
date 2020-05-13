// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Stride.Shaders.Parser.Mixins
{
    public class ShaderKeyFileHelper
    {
        public static byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            // Always output a result into the file
            string result;
            try
            {
                Stride.Core.Shaders.Parser.ShaderMacro[] macros;

                // Changed some keywords to avoid ambiguities with HLSL and improve consistency
                if (inputFileName != null && Path.GetExtension(inputFileName).ToLowerInvariant() == ".sdfx")
                {
                    // XKFX
                    macros = new[]
                    {
                        new Core.Shaders.Parser.ShaderMacro("shader", "effect")
                    };
                }
                else
                {
                    // XKSL
                    macros = new[]
                    {
                        new Core.Shaders.Parser.ShaderMacro("class", "shader")
                    };
                }

                var parsingResult = StrideShaderParser.TryPreProcessAndParse(inputFileContent, inputFileName, macros);

                if (parsingResult.HasErrors)
                {
                    result = "// Failed to parse the shader:\n" + parsingResult;
                }
                else
                {
                    // Try to generate a mixin code.
                    var shaderKeyGenerator = new ShaderMixinCodeGen(parsingResult.Shader, parsingResult);

                    shaderKeyGenerator.Run();
                    result = shaderKeyGenerator.Text ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                result = "// Unexpected exceptions occurred while generating the file\n" + ex;
            }

            // We force the UTF8 to include the BOM to match VS default
            var data = Encoding.UTF8.GetBytes(result);
            return Encoding.UTF8.GetPreamble().Concat(data).ToArray();
        }
    }
}
