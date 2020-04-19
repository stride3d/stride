// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Irony.Parsing;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Grammar;
using Stride.Core.Shaders.Utility;

using SourceLocation = Stride.Core.Shaders.Ast.SourceLocation;
using SourceSpan = Stride.Core.Shaders.Ast.SourceSpan;

namespace Stride.Core.Shaders.Parser
{
    /// <summary>
    /// Parser class.
    /// </summary>
    public class ShaderParser
    {
        private static readonly Dictionary<Type, ShaderLanguageData> LanguageDatas = new Dictionary<Type, ShaderLanguageData>();

        /// <summary>
        /// Gets or sets the parser.
        /// </summary>
        /// <value>
        /// The parser.
        /// </value>
        public Irony.Parsing.Parser Parser { get; private set; }

        /// <summary>
        /// Gets the grammar.
        /// </summary>
        public ShaderGrammar Grammar{ get; private set; }

        /// <summary>
        /// Gets or sets the language data.
        /// </summary>
        /// <value>
        /// The language data.
        /// </value>
        public ShaderLanguageData LanguageData { get; private set; }

        /// <summary>
        /// Gets the tokenizer.
        /// </summary>
        public Tokenizer Tokenizer { get; private set; }

        /// <summary>
        /// Prevents a default instance of the <see cref="ShaderParser"/> class from being created.
        /// </summary>
        /// <param name="languageData">The language data.</param>
        /// <param name="root">The root of the language.</param>
        private ShaderParser(ShaderLanguageData languageData, NonTerminal root)
        {
            LanguageData = languageData;
            Grammar = (ShaderGrammar)languageData.Grammar;
            Tokenizer = new Tokenizer(languageData);
            Parser = new Irony.Parsing.Parser(languageData, null, root);
        }

        /// <summary>
        /// Preprocesses and parses the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <param name="macros">The macros defined for the preprocessor.</param>
        /// <param name="includeDirectories">The include directories used by the preprocessor..</param>
        /// <returns>Result of parsing</returns>
        public ParsingResult TryPreProcessAndParse(string source, string sourceFileName, ShaderMacro[] macros = null, params string[] includeDirectories)
        {

            var allIncludeDirectories = new List<string>();

            if (includeDirectories != null)
                allIncludeDirectories.AddRange(includeDirectories);

            var directoryName = Path.GetDirectoryName(sourceFileName);
            if (!string.IsNullOrEmpty(directoryName))
                allIncludeDirectories.Add(directoryName);

            // Run the processor
            string preprocessedSource;

            try
            {
                preprocessedSource = PreProcessor.Run(source, sourceFileName, macros, allIncludeDirectories.ToArray());
            }
            catch (Exception ex)
            {
                var result = new ParsingResult();
                result.Error(MessageCode.ErrorUnexpectedException, new SourceSpan(new SourceLocation(sourceFileName, 0, 1, 1), 1), ex);
                return result;
            }

            // Parse the source
            return Parse(preprocessedSource, sourceFileName);
        }

        /// <summary>
        /// Preprocesses and parses the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <param name="macros">The macros defined for the preprocessor.</param>
        /// <param name="includeDirectories">The include directories used by the preprocessor..</param>
        /// <returns>Result of parsing</returns>
        public Shader PreProcessAndParse(string source, string sourceFileName, ShaderMacro[] macros = null, params string[] includeDirectories)
        {
            // Parse the source
            var result = TryPreProcessAndParse(source, sourceFileName, macros, includeDirectories);
            return Check(result, sourceFileName);
        }

        /// <summary>
        /// Gets the parser.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ShaderParser GetParser<T>(NonTerminal root = null) where T : ShaderGrammar, new()
        {
            ShaderLanguageData languageData;
            lock (LanguageDatas)
            {
                if (!LanguageDatas.TryGetValue(typeof(T), out languageData))
                {
                    languageData = new ShaderLanguageData(new T());
                    LanguageDatas.Add(typeof(T), languageData);
                }
            }

            return new ShaderParser(languageData, root);
        }

        /// <summary>
        /// Gets the language.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetGrammar<T>() where T : ShaderGrammar, new()
        {
            ShaderLanguageData languageData;
            lock (LanguageDatas)
            {
                if (!LanguageDatas.TryGetValue(typeof(T), out languageData))
                {
                    languageData = new ShaderLanguageData(new T());
                    LanguageDatas.Add(typeof(T), languageData);
                }

                return (T)languageData.Grammar;
            }
        }

        /// <summary>
        /// Parses the specified source code.
        /// </summary>
        /// <typeparam name="T">Type of the grammar</typeparam>
        /// <param name="sourceCode">The source code.</param>
        /// <param name="file">The file.</param>
        /// <returns>A parsing result</returns>
        public ParsingResult Parse(string sourceCode, string file)
        {
            var clock = new Stopwatch();
            clock.Start();
            var parseTree = Parser.Parse(sourceCode, file);
            clock.Stop();

            var result = new ParsingResult
                {
                    TimeToParse = clock.ElapsedMilliseconds,
                    TokenCount = parseTree.Tokens.Count,
                };

            // Get the parsed node
            if (parseTree.Root != null && parseTree.Root.AstNode != null)
            {
                result.Shader = (Shader)((IronyBrowsableNode)parseTree.Root.AstNode).Node;
            } 
            else
            {
                result.HasErrors = true;
            }


            // Add messages from Irony
            HandleMessages(parseTree, file, result);

            return result;
        }

        /// <summary>
        /// Parse a source code file and check that the result is valid.
        /// </summary>
        /// <param name="sourceCode"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public Shader ParseAndCheck(string sourceCode, string file)
        {
            var result = Parse(sourceCode, file);
            return Check(result, file);
        }

        public static Shader Check(ParsingResult result, string sourceFileName)
        {
            // Throws an exception if there are any errors.
            // Todo provide better handling 
            if (result.HasErrors)
            {
                var errorText = new StringBuilder();
                errorText.AppendFormat("Unable to parse file [{0}]", sourceFileName).AppendLine();
                foreach (var reportMessage in result.Messages)
                {
                    errorText.AppendLine(reportMessage.ToString());
                }
                throw new InvalidOperationException(errorText.ToString());
            }
            return result.Shader;
        }

        private static void HandleMessages(ParseTree parseTree, string file, ParsingResult result)
        {
            foreach (var parserMessage in parseTree.ParserMessages)
            {
                var level = new ReportMessageLevel();
                switch (parserMessage.Level)
                {
                    case ParserErrorLevel.Info:
                        level = ReportMessageLevel.Info;
                        break;
                    case ParserErrorLevel.Error:
                        level = ReportMessageLevel.Error;
                        break;
                    case ParserErrorLevel.Warning:
                        level = ReportMessageLevel.Warning;
                        break;
                }

                result.Messages.Add(new ReportMessage(level, "", parserMessage.Message, new Ast.SourceSpan(SpanConverter.Convert(parserMessage.Location), 0)));

                if (parserMessage.Level != ParserErrorLevel.Info) result.HasErrors = true;
            }
        }
    }
}
