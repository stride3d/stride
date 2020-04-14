// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Stride.Shaders.Parser.Mixins;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Utility;

namespace Stride.Shaders.Parser
{
    /// <summary>
    /// This class helps to navigate from a text location and try to find the associated definition location 
    /// (local variables, stage variables, class sdsl, shaders sdfx...etc.)
    /// </summary>
    public class ShaderNavigation
    {
        private static readonly MessageCode ErrorMixinNotFound = new MessageCode("E900", "Mixin [{0}] not found from results");

        /// <summary>
        /// Analyzes the shader source code and go to definition.
        /// </summary>
        /// <param name="shaderSource">The shader source.</param>
        /// <param name="location">The location.</param>
        /// <param name="shaderDirectories">The shader directories.</param>
        /// <returns>ShaderNavigationResult.</returns>
        /// <exception cref="System.ArgumentNullException">shaderSource</exception>
        /// <exception cref="System.ArgumentException">Expecting a FileSource location;location</exception>
        public ShaderNavigationResult AnalyzeAndGoToDefinition(string shaderSource, Stride.Core.Shaders.Ast.SourceLocation location, List<string> shaderDirectories)
        {
            if (shaderSource == null) throw new ArgumentNullException("shaderSource");
            var navigationResult = new ShaderNavigationResult();
            if (location.FileSource == null)
            {
                throw new ArgumentException("Expecting a FileSource location", "location");
            }

            try
            {
                if (location.FileSource.EndsWith(".sdsl", StringComparison.CurrentCultureIgnoreCase))
                {
                    AnalyzeAndGoToDefinition(shaderSource, location, shaderDirectories, navigationResult);
                }

                // If any of the messages have empty filesource, add a default filesource
                foreach (var message in navigationResult.Messages.Messages)
                {
                    if (string.IsNullOrWhiteSpace(message.Span.Location.FileSource))
                    {
                        message.Span.Location.FileSource = location.FileSource;
                    }
                }
            }
            catch (Exception ex)
            {
                navigationResult.Messages.Error(MessageCode.ErrorUnexpectedException, GetSpan(location), ex);
            }

            return navigationResult;
        }

        private static SourceSpan GetSpan(SourceLocation location)
        {
            return new SourceSpan(new SourceLocation(location.FileSource, 0, 1, 1), 1);
        }

        private void AnalyzeAndGoToDefinition(string shaderSource, Stride.Core.Shaders.Ast.SourceLocation location, List<string> shaderDirectories, ShaderNavigationResult result)
        {
            // We are not using the storage when loading shaders from VS but directly the filesystem
            var mixer = new ShaderMixinParser(null);
            mixer.SourceManager.UseFileSystem = true;
            mixer.AllowNonInstantiatedGenerics = true;
            mixer.SourceManager.LookupDirectoryList.AddRange(shaderDirectories);

            var shaderSourceName = Path.GetFileNameWithoutExtension(location.FileSource);
            mixer.SourceManager.AddShaderSource(shaderSourceName, shaderSource, location.FileSource);

            var mixinSource = new ShaderMixinSource();
            mixinSource.Mixins.Add(new ShaderClassSource(shaderSourceName));

            ShaderMixinParsingResult parsingResult;
            HashSet<ModuleMixinInfo> moduleMixins;
            var mixerResult = mixer.ParseAndAnalyze(mixinSource, null, out parsingResult, out moduleMixins);

            // Copy shader analysis to result
            parsingResult.CopyTo(result.Messages);

            if (mixerResult == null)
            {
                return;
            }

            var mixin = mixerResult.MixinInfos.FirstOrDefault(item => item.MixinName == shaderSourceName);

            if (mixin == null)
            {
                result.Messages.Error(ErrorMixinNotFound, GetSpan(location), shaderSourceName);
                return;
            }

            // If first line, first column, this is not a go to definition but only parsing request, so return directly
            if (location.Line == 1 && location.Column == 1)
            {
                return;
            }

            // var ast = mixin.MixinAst;

            var parsingInfo = mixin.Mixin.ParsingInfo;

            var pools = new List<ReferencesPool>
                {
                    parsingInfo.ClassReferences,
                    parsingInfo.StaticReferences,
                    parsingInfo.ExternReferences,
                    parsingInfo.StageInitReferences,
                };

            foreach (var pool in pools)
            {
                var span = Find(pool, location);
                if (span.HasValue)
                {
                    result.DefinitionLocation = span.Value;
                    return;
                }
            }

            // Else Try to find from remaining navigable nodes
            foreach (var node in parsingInfo.NavigableNodes)
            {
                if (IsExpressionMatching(node, location))
                {
                    var typeReferencer = node as ITypeInferencer;
                    if (typeReferencer != null && typeReferencer.TypeInference != null && typeReferencer.TypeInference.Declaration != null)
                    {
                        var declarationNode = (Node)typeReferencer.TypeInference.Declaration;
                        result.DefinitionLocation = declarationNode.Span;
                        break;
                    }
                }
            }
        }

        private SourceSpan? Find(ReferencesPool pool, Stride.Core.Shaders.Ast.SourceLocation location)
        {
            foreach (var methodRef in pool.MethodsReferences)
            {
                foreach (var expression in methodRef.Value)
                {
                    if (IsExpressionMatching(expression, location))
                    {
                        return methodRef.Key.Span;
                    }
                }
            }

            foreach (var variableRef in pool.VariablesReferences)
            {
                foreach (var expression in variableRef.Value)
                {
                    if (IsExpressionMatching(expression.Expression, location))
                    {
                        return variableRef.Key.Span;
                    }
                }
            }
            return null;
        }

        private bool IsExpressionMatching(Node astNode, Stride.Core.Shaders.Ast.SourceLocation location)
        {
            var span = astNode.Span;
            var startColumn = span.Location.Column;
            var endColumn = startColumn + span.Length;
            if (astNode.Span.Location.Line == location.Line && location.Column >= startColumn)
            {
                if (location.Column >= startColumn && location.Column <= endColumn)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
