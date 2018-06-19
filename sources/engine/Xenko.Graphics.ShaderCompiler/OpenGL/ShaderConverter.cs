// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_WINDOWS_DESKTOP
using System;
using System.Collections.Generic;
using System.IO;
using Xenko.Core.Shaders.Analysis.Hlsl;
using Xenko.Core.Shaders.Convertor;
using Xenko.Core.Shaders.Parser;
using Xenko.Core.Shaders.Parser.Hlsl;

using ShaderMacro = Xenko.Shaders.ShaderMacro;

namespace Xenko.Graphics.ShaderCompiler.OpenGL
{
    /// <summary>
    /// Converts from HLSL shader sourcecode to a GLSL sourcecode.
    /// </summary>
    internal class ShaderConverter
    {
        private bool isOpenGLES;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderConverter"/> class.
        /// </summary>
        public ShaderConverter(bool isOpenGLES)
        {
            this.isOpenGLES = isOpenGLES;
            Log = Console.Out;
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
        /// Gets or sets the log used by this instance.
        /// </summary>
        /// <value>
        /// The log used by this instance.
        /// </value>
        public TextWriter Log { get; set; }

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
        public global::Xenko.Core.Shaders.Ast.Shader Convert(string hlslSourcecode, string hlslEntryPoint, PipelineStage stage, string inputHlslFilepath = null)
        {
            try
            {
                // Convert from Framework.Graphics ShaderMacro to Framework.Shaders ShaderMacro
                var macros = new global::Xenko.Core.Shaders.Parser.ShaderMacro[Macros.Count];
                for (int index = 0; index < Macros.Count; index++)
                    macros[index] = new global::Xenko.Core.Shaders.Parser.ShaderMacro(Macros[index].Name, Macros[index].Definition);

                var result = HlslParser.TryPreProcessAndParse(hlslSourcecode, inputHlslFilepath, macros, IncludeDirectories);

                if (result.HasErrors)
                {
                    throw new NotImplementedException("Logging");
                    //DisplayError(log, result, "Error while parsing file:");
                    return null;
                }

                // Prepare the shader before type inference analysis
                HlslToGlslConvertor.Prepare(result.Shader);

                HlslSemanticAnalysis.Run(result);

                // If there are any type inference analysis, just display all errors but ytu
                if (result.HasErrors)
                {
                    throw new NotImplementedException("Logging");
                    //DisplayError(log, result, "Error with type inferencing:");
                }

                return Convert(result, hlslEntryPoint, stage, inputHlslFilepath);
            }
            catch (Exception ex)
            {
                throw new NotImplementedException("Logging");
                //log.WriteLine("Unexpected error while converting file [{0}] with entry point [{1}] : {2}", inputHlslFilepath, hlslEntryPoint, ex.Message);
                return null;
            }
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
        private global::Xenko.Core.Shaders.Ast.Shader Convert(ParsingResult result, string hlslEntryPoint, PipelineStage stage, string inputHlslFilepath = null)
        {
            try
            {
                var convertor = new HlslToGlslConvertor(hlslEntryPoint, stage, ShaderModel.Model40) // TODO HARDCODED VALUE to change
                {
                    KeepConstantBuffer = !isOpenGLES,
                    TextureFunctionsCompatibilityProfile = isOpenGLES,
                    NoSwapForBinaryMatrixOperation = true,
                    UseBindingLayout = false,
                    UseLocationLayout = false,
                    UseSemanticForVariable = true,
                    IsPointSpriteShader = false,
                    ViewFrustumRemap = true,
                    KeepNonUniformArrayInitializers = !isOpenGLES
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
                throw new NotImplementedException("Logging");
                //log.WriteLine("Unexpected error while converting file [{0}] with entry point [{1}] : {2}", inputHlslFilepath, hlslEntryPoint, ex.Message);
                return null;
            }
        }
    }
}
#endif
