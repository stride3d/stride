// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Stride.Core.Shaders.Ast.Stride;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Grammar.Stride;
using Stride.Core.Shaders.Parser;
using Stride.Core.Shaders.Visitor;

namespace Stride.Shaders.Parser
{
    /// <summary>
    /// Main class for parsing Stride HLSL grammar.
    /// </summary>
    public static class StrideShaderParser
    {
        /// <summary>
        /// Preinitialize the parser.
        /// </summary>
        public static void Initialize()
        {
            ShaderParser.GetParser<StrideGrammar>();
        }

        /// <summary>
        /// Preprocesses and parses the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <param name="macros">The macros defined for the preprocessor.</param>
        /// <param name="includeDirectories">The include directories used by the preprocessor..</param>
        /// <returns>Result of parsing</returns>
        public static ParsingResult TryPreProcessAndParse(string source, string sourceFileName, Stride.Core.Shaders.Parser.ShaderMacro[] macros = null, params string[] includeDirectories)
        {

            var result = ShaderParser.GetParser<StrideGrammar>().TryPreProcessAndParse(source, sourceFileName, macros, includeDirectories);
            PrepareShader(result.Shader);
            return result;
        }

        /// <summary>
        /// Preprocesses and parses the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <param name="macros">The macros defined for the preprocessor.</param>
        /// <param name="includeDirectories">The include directories used by the preprocessor..</param>
        /// <returns>Result of parsing</returns>
        public static Shader PreProcessAndParse(string source, string sourceFileName, Stride.Core.Shaders.Parser.ShaderMacro[] macros = null, params string[] includeDirectories)
        {
            return PrepareShader(ShaderParser.GetParser<StrideGrammar>().PreProcessAndParse(source, sourceFileName, macros, includeDirectories));
        }

        /// <summary>
        /// Preprocesses and parses the specified source.
        /// </summary>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <param name="macros">The macros defined for the preprocessor.</param>
        /// <param name="includeDirectories">The include directories used by the preprocessor..</param>
        /// <returns>Result of parsing</returns>
        public static ParsingResult TryPreProcessAndParse(string sourceFileName, Stride.Core.Shaders.Parser.ShaderMacro[] macros = null, params string[] includeDirectories)
        {
            var result = TryPreProcessAndParse(File.ReadAllText(sourceFileName), sourceFileName, macros, includeDirectories);
            return result;
        }

        /// <summary>
        /// Preprocesses and parses the specified source.
        /// </summary>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <param name="macros">The macros defined for the preprocessor.</param>
        /// <param name="includeDirectories">The include directories used by the preprocessor..</param>
        /// <returns>Result of parsing</returns>
        public static Shader PreProcessAndParse(string sourceFileName, Stride.Core.Shaders.Parser.ShaderMacro[] macros = null, params string[] includeDirectories)
        {
            return PreProcessAndParse(File.ReadAllText(sourceFileName), sourceFileName, macros, includeDirectories);
        }

        /// <summary>
        /// Parses the specified source code.
        /// </summary>
        /// <param name="sourceCode">The source code.</param>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <returns></returns>
        public static Shader Parse(string sourceCode, string sourceFileName)
        {
            return PrepareShader(ShaderParser.GetParser<StrideGrammar>().ParseAndCheck(sourceCode, sourceFileName));
        }

        /// <summary>
        /// Parses the specified source code.
        /// </summary>
        /// <param name="sourceCode">The source code.</param>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <returns></returns>
        public static ParsingResult TryParse(string sourceCode, string sourceFileName)
        {
            var parsingResult = ShaderParser.GetParser<StrideGrammar>().Parse(sourceCode, sourceFileName);
            PrepareShader(parsingResult.Shader);
            return parsingResult;
        }

        private static Shader PrepareShader(Shader shader)
        {
            if (shader == null)
            {
                return null;
            }

            // Replace all variable in constant buffers by single variable with a constant buffer tag 
            foreach (var classType in GetShaderClassTypes(shader.Declarations))
            {
                var members = new List<Node>();
                foreach (var member in classType.Members)
                {
                    var constantBuffer = member as ConstantBuffer;
                    if (constantBuffer != null)
                    {
                        var variables = new List<Node>();
                        for (var index = 0; index < constantBuffer.Members.Count; index++)
                        {
                            var variable = constantBuffer.Members[index] as Variable;
                            if (variable != null)
                            {
                                foreach (var subVariable in variable.Instances())
                                {
                                    subVariable.SetTag(StrideTags.ConstantBuffer, constantBuffer);
                                    subVariable.SetTag(StrideTags.ConstantBufferIndex, index);
                                    if (variable.IsGroup && !ReferenceEquals(variable, subVariable))
                                    {
                                        subVariable.Qualifiers |= variable.Qualifiers;
                                        subVariable.Attributes.AddRange(variable.Attributes);
                                    }

                                    variables.Add(subVariable);
                                }
                            }
                        }

                        members.AddRange(variables);
                    } 
                    else
                    {
                        members.Add(member);
                    }
                }

                classType.Members = members;
            }
            return shader;
        }

        internal static IEnumerable<ShaderClassType> GetShaderClassTypes(IEnumerable<Stride.Core.Shaders.Ast.Node> nodes)
        {
            foreach (var node in nodes)
            {
                var namespaceBlock = node as NamespaceBlock;
                if (namespaceBlock != null)
                {
                    foreach (var type in GetShaderClassTypes(namespaceBlock.Body))
                    {
                        yield return type;
                    }
                }
                else
                {
                    var shaderClass = node as ShaderClassType;
                    if (shaderClass != null)
                    {
                        yield return shaderClass;
                    }
                }
            }
        }
    }
}
