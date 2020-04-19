// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.Shaders.Analysis.Hlsl;
using Stride.Core.Shaders.Convertor;
using Stride.Core.Shaders.Parser;
using Stride.Core.Shaders.Parser.Hlsl;
using ShaderMacro = Stride.Shaders.ShaderMacro;

namespace Stride.Shaders.Compiler.OpenGL
{
    /// <summary>
    /// Converts from HLSL shader sourcecode to a GLSL sourcecode.
    /// </summary>
    internal class ShaderConverter
    {
        private GlslShaderPlatform shaderPlatform;
        private int shaderVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderConverter"/> class.
        /// </summary>
        public ShaderConverter(GlslShaderPlatform shaderPlatform, int shaderVersion)
        {
            this.shaderPlatform = shaderPlatform;
            this.shaderVersion = shaderVersion;

            IsVerboseLog = true;
            Macros = new List<ShaderMacro>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is producing a verbose log.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is producing a verbose log; otherwise, <c>false</c>.
        /// </value>
        public bool IsVerboseLog { get; set; }

        /// <summary>
        /// Gets or sets the include directories.
        /// </summary>
        /// <value>
        /// The include directories.
        /// </value>
        public string[] IncludeDirectories { get; set; }


        /// <summary>
        /// Gets or sets the macros.
        /// </summary>
        /// <value>
        /// The macros.
        /// </value>
        public List<ShaderMacro> Macros { get; set; }

        /// <summary>
        /// Converts the specified hlsl source code to glsl.
        /// </summary>
        /// <param name="hlslSourcecode">The HLSL source code.</param>
        /// <param name="hlslEntryPoint">The HLSL entry point.</param>
        /// <param name="stage">The stage to convert.</param>
        /// <param name="shader">The shader.</param>
        /// <param name="inputHlslFilepath">The input HLSL filepath.</param>
        /// <returns>
        /// The resulting glsl AST tree.
        /// </returns>
        public global::Stride.Core.Shaders.Ast.Shader Convert(string hlslSourcecode, string hlslEntryPoint, PipelineStage stage, string inputHlslFilepath, IDictionary<int, string> inputAttributeNames, LoggerResult log)
        {
            try
            {
                // Convert from Framework.Graphics ShaderMacro to Framework.Shaders ShaderMacro
                var macros = new global::Stride.Core.Shaders.Parser.ShaderMacro[Macros.Count];
                for (int index = 0; index < Macros.Count; index++)
                    macros[index] = new global::Stride.Core.Shaders.Parser.ShaderMacro(Macros[index].Name, Macros[index].Definition);

                var result = HlslParser.TryPreProcessAndParse(hlslSourcecode, inputHlslFilepath, macros, IncludeDirectories);

                if (result.HasErrors)
                {
                    log.Error(result.ToString());
                    return null;
                }

                // Prepare the shader before type inference analysis
                HlslToGlslConvertor.Prepare(result.Shader);

                HlslSemanticAnalysis.Run(result);

                // If there are any type inference analysis, just display all errors but ytu
                if (result.HasErrors)
                {
                    log.Error(result.ToString());
                    return null;
                }

                return Convert(result, hlslEntryPoint, stage, inputHlslFilepath, inputAttributeNames, log);
            }
            catch (Exception ex)
            {
                log.Error($"Unexpected error while converting file [{inputHlslFilepath}] with entry point [{hlslEntryPoint}]", ex);
            }
            return null;
        }

        /// <summary>
        /// Converts the specified hlsl source code to glsl.
        /// </summary>
        /// <param name="hlslEntryPoint">The HLSL entry point.</param>
        /// <param name="stage">The stage to convert.</param>
        /// <param name="shader">The shader.</param>
        /// <param name="inputHlslFilepath">The input HLSL filepath.</param>
        /// <returns>
        /// The resulting glsl AST tree.
        /// </returns>
        private global::Stride.Core.Shaders.Ast.Shader Convert(ParsingResult result, string hlslEntryPoint, PipelineStage stage, string inputHlslFilepath, IDictionary<int, string> inputAttributeNames, LoggerResult log)
        {
            try
            {
                var convertor = new HlslToGlslConvertor(shaderPlatform, shaderVersion, hlslEntryPoint, stage, ShaderModel.Model40) // TODO HARDCODED VALUE to change
                {
                    // Those settings are now default values
                    //NoSwapForBinaryMatrixOperation = true,
                    //UnrollForLoops = true,
                    //ViewFrustumRemap = true,
                    //FlipRenderTarget = true,
                    //KeepConstantBuffer = !isOpenGLES || isOpenGLES3,
                    //TextureFunctionsCompatibilityProfile = isOpenGLES && !isOpenGLES3,
                    //KeepNonUniformArrayInitializers = !isOpenGLES,

                    UseBindingLayout = false,
                    UseSemanticForVariable = true,
                    IsPointSpriteShader = false,
                    InputAttributeNames = inputAttributeNames
                };
                convertor.Run(result);

                // After the converter we display the errors but we don't stop writing output glsl
                if (result.HasErrors)
                {
                    //DisplayError(log, result, "Error while converting file:");
                }


                return result.Shader;
            }
            catch (Exception ex)
            {
                log.Error($"Unexpected error while converting file [{inputHlslFilepath}] with entry point [{hlslEntryPoint}]", ex);
                return null;
            }
        }
    }
}
