// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.IO;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Shaders.Parser.Mixins;
using Stride.Shaders.Parser.Utility;
using Stride.Core.Shaders.Analysis.Hlsl;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Utility;

namespace Stride.Shaders.Parser
{
    /// <summary>
    /// Parser for mixin.
    /// </summary>
    public class ShaderMixinParser
    {
        #region Private members

        /// <summary>
        /// An Objbect to lock the preprocess step (virtual tables building etc.).
        /// </summary>
        private static readonly Object PreprocessLock = new Object();

        /// <summary>
        /// An Objbect to lock the semantic analysis step.
        /// </summary>
        private static readonly Object SemanticAnalyzerLock = new Object();

        /// <summary>
        /// The CloneContext with the Hlsl classes and types
        /// </summary>
        private CloneContext hlslCloneContext;
        private object hlslCloneContextLock = new object();

        /// <summary>
        /// The library containing all the shaders
        /// </summary>
        private readonly StrideShaderLibrary shaderLibrary;

        #endregion

        #region Public members

        /// <summary>
        /// The shader source manager.
        /// </summary>
        public readonly ShaderSourceManager SourceManager;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderMixinParser"/> class.
        /// </summary>
        public ShaderMixinParser(IVirtualFileProvider fileProvider)
        {
            SourceManager = new ShaderSourceManager(fileProvider);
            var shaderLoader = new ShaderLoader(SourceManager);

            if (shaderLibrary == null)
            {
                shaderLibrary = new StrideShaderLibrary(shaderLoader);
            }
        }

        #endregion

        #region Public method

        /// <summary>
        /// Deletes the shader cache for the specified shaders.
        /// </summary>
        /// <param name="modifiedShaders">The modified shaders.</param>
        public void DeleteObsoleteCache(HashSet<string> modifiedShaders)
        {
            lock (shaderLibrary)
            {
                shaderLibrary.DeleteObsoleteCache(modifiedShaders);
            }
        }
        public bool AllowNonInstantiatedGenerics
        {
            get
            {
                return shaderLibrary.AllowNonInstantiatedGenerics;
            }
            set
            {
                shaderLibrary.AllowNonInstantiatedGenerics = value;
            }
        }

        internal ShaderCompilationContext ParseAndAnalyze(ShaderMixinSource shaderMixinSource, Stride.Shaders.ShaderMacro[] macros, out ShaderMixinParsingResult parsingResult, out HashSet<ModuleMixinInfo> mixinsToAnalyze)
        {
            // Creates a parsing result
            parsingResult = new ShaderMixinParsingResult();

            Stride.Core.Shaders.Parser.ShaderMacro[] macrosParser;
            if (macros == null)
            {
                macrosParser = new Stride.Core.Shaders.Parser.ShaderMacro[0];
            }
            else
            {
                macrosParser = new Stride.Core.Shaders.Parser.ShaderMacro[macros.Length];
                for (var i = 0; i < macros.Length; ++i)
                    macrosParser[i] = new Stride.Core.Shaders.Parser.ShaderMacro(macros[i].Name, macros[i].Definition);
            }
            //PerformanceLogger.Start(PerformanceStage.Global);

            // ----------------------------------------------------------
            // Load all shaders
            // ----------------------------------------------------------
            lock (shaderLibrary)
            {
                //PerformanceLogger.Start(PerformanceStage.Loading);
                mixinsToAnalyze = shaderLibrary.LoadShaderSource(shaderMixinSource, macrosParser);
                //PerformanceLogger.Stop(PerformanceStage.Loading);
            }

            // Extract all ModuleMixinInfo and check for any errors
            var allMixinInfos = new HashSet<ModuleMixinInfo>();
            foreach (var moduleMixinInfo in mixinsToAnalyze)
            {
                allMixinInfos.UnionWith(moduleMixinInfo.MinimalContext);
            }
            foreach (var moduleMixinInfo in allMixinInfos)
            {
                moduleMixinInfo.Log.CopyTo(parsingResult);

                var ast = moduleMixinInfo.MixinAst;
                var shaderClassSource = moduleMixinInfo.ShaderSource as ShaderClassCode;
                // If we have a ShaderClassSource and it is not an inline one, then we can store the hash sources
                if (ast != null && shaderClassSource != null)
                {
                    parsingResult.HashSources[shaderClassSource.ClassName] = moduleMixinInfo.SourceHash;
                }
            }

            // Return directly if there was any errors
            if (parsingResult.HasErrors)
                return null;

            // ----------------------------------------------------------
            // Perform Type Analysis
            // ----------------------------------------------------------
            //PerformanceLogger.Start(PerformanceStage.TypeAnalysis);
            var context = GetCompilationContext(mixinsToAnalyze, parsingResult);
            //PerformanceLogger.Stop(PerformanceStage.TypeAnalysis);

            // Return directly if there was any errors
            if (parsingResult.HasErrors)
                return context;

            lock (SemanticAnalyzerLock)
            {
                //PerformanceLogger.Start(PerformanceStage.SemanticAnalysis);
                //SemanticPerformance.Start(SemanticStage.Global);
                foreach (var mixin in mixinsToAnalyze)
                    context.Analyze(mixin);
                //SemanticPerformance.Pause(SemanticStage.Global);
                //PerformanceLogger.Stop(PerformanceStage.SemanticAnalysis);
            }

            return context;
        }

        /// <summary>
        /// Mixes shader parts to produces a single HLSL file shader.
        /// </summary>
        /// <param name="shaderMixinSource">The shader source.</param>
        /// <param name="macros">The shader perprocessor macros.</param>
        /// <param name="modifiedShaders">The list of modified shaders.</param>
        /// <returns>The combined shader in AST form.</returns>
        public ShaderMixinParsingResult Parse(ShaderMixinSource shaderMixinSource, Stride.Shaders.ShaderMacro[] macros = null)
        {
            // Make in-memory shader classes known to the source manager
            foreach (var x in shaderMixinSource.Mixins.OfType<ShaderClassString>())
                SourceManager.AddShaderSource(x.ClassName, x.ShaderSourceCode, x.ClassName);

            // Creates a parsing result
            HashSet<ModuleMixinInfo> mixinsToAnalyze;
            ShaderMixinParsingResult parsingResult;
            var context = ParseAndAnalyze(shaderMixinSource, macros, out parsingResult, out mixinsToAnalyze);

            // Return directly if there was any errors
            if (parsingResult.HasErrors)
                return parsingResult;

            // Update the clone context in case new instances of classes are created
            CloneContext mixCloneContext;

            lock (hlslCloneContextLock)
            {
                if (hlslCloneContext == null)
                {
                    hlslCloneContext = new CloneContext();

                    // Create the clone context with the instances of Hlsl classes
                    HlslSemanticAnalysis.FillCloneContext(hlslCloneContext);
                }

                HlslSemanticAnalysis.UpdateCloneContext(hlslCloneContext);
                mixCloneContext = new CloneContext(hlslCloneContext);
            }

            // only clone once the stage classes
            foreach (var mixinInfo in mixinsToAnalyze)
            {
                foreach (var mixin in mixinInfo.Mixin.MinimalContext.Where(x => x.StageOnlyClass))
                {
                    mixin.DeepClone(mixCloneContext);
                }
            }

            // ----------------------------------------------------------
            // Perform Shader Mixer
            // ----------------------------------------------------------
            var externDict = new CompositionDictionary();
            var finalModuleList = BuildCompositionsDictionary(shaderMixinSource, externDict, context, mixCloneContext, parsingResult);
            //PerformanceLogger.Stop(PerformanceStage.DeepClone);

            if (parsingResult.HasErrors)
                return parsingResult;

            // look for stage compositions and add the links between variables and compositions when necessary
            var extraExternDict = new Dictionary<Variable, List<ModuleMixin>>();
            foreach (var item in externDict)
            {
                if (item.Key.Qualifiers.Contains(StrideStorageQualifier.Stage))
                    FullLinkStageCompositions(item.Key, item.Value, externDict, extraExternDict, parsingResult);
            }
            foreach (var item in extraExternDict)
                externDict.Add(item.Key, item.Value);

            var mixinDictionary = BuildMixinDictionary(finalModuleList);

            if (finalModuleList != null)
            {
                var finalModule = finalModuleList[0];
                //PerformanceLogger.Start(PerformanceStage.Mix);
                parsingResult.Reflection = new EffectReflection();
                var mixer = new StrideShaderMixer(finalModule, parsingResult, mixinDictionary, externDict, new CloneContext(mixCloneContext));
                mixer.Mix();
                //PerformanceLogger.Stop(PerformanceStage.Mix);

                // Return directly if there was any errors
                if (parsingResult.HasErrors)
                    return parsingResult;

                var finalShader = mixer.GetMixedShader();

                // Simplifies the shader by removing dead code
                var simplifier = new ExpressionSimplifierVisitor();
                simplifier.Run(finalShader);

                var sdShaderLinker = new ShaderLinker(parsingResult);
                sdShaderLinker.Run(finalShader);

                // Return directly if there was any errors
                if (parsingResult.HasErrors)
                    return parsingResult;

                // Find all entry points
                // TODO: make this configurable by CompileParameters
                foreach (var stage in new[] {ShaderStage.Compute, ShaderStage.Vertex, ShaderStage.Hull, ShaderStage.Domain, ShaderStage.Geometry, ShaderStage.Pixel})
                {
                    var entryPoint = finalShader.Declarations.OfType<MethodDefinition>().FirstOrDefault(f => f.Attributes.OfType<AttributeDeclaration>().Any(a => a.Name == "EntryPoint" && (string)a.Parameters[0].Value == stage.ToString()));

                    if (entryPoint == null)
                    {
                        continue;
                    }

                    parsingResult.EntryPoints[stage] = entryPoint.Name.Text;

                    // When this is a compute shader, there is no need to scan other stages
                    if (stage == ShaderStage.Compute)
                        break;
                }

                var typeCleaner = new StrideShaderCleaner();
                typeCleaner.Run(finalShader);

                //PerformanceLogger.Stop(PerformanceStage.Global);

                //PerformanceLogger.PrintLastResult();
                //SemanticPerformance.PrintResult();
                //MixPerformance.PrintResult();
                //GenerateShaderPerformance.PrintResult();
                //StreamCreatorPerformance.PrintResult();
                //ShaderLoader.PrintTime();

                //PerformanceLogger.WriteOut(52);

                parsingResult.Shader = finalShader;
            }

            return parsingResult;
        }

        #endregion

        #region Internal methods

        internal ModuleMixinInfo GetMixin(string mixinName)
        {
            return shaderLibrary.MixinInfos.FirstOrDefault(x => x.MixinGenericName == mixinName);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// create the context for each composition by cloning their dependencies
        /// </summary>
        /// <param name="shaderSource">the entry ShaderSource (root)</param>
        /// <param name="dictionary">the ouputed compositions</param>
        /// <param name="compilationContext">the compilation context</param>
        /// <param name="cloneContext">The clone context.</param>
        /// <returns>a list of all the needed mixins</returns>
        private static List<ModuleMixin> BuildCompositionsDictionary(ShaderSource shaderSource, CompositionDictionary dictionary, ShaderCompilationContext compilationContext, CloneContext cloneContext, LoggerResult log)
        {
            if (shaderSource is ShaderMixinSource)
            {
                var shaderMixinSource = shaderSource as ShaderMixinSource;

                var finalModule = compilationContext.GetModuleMixinFromShaderSource(shaderSource);

                //PerformanceLogger.Start(PerformanceStage.DeepClone);
                finalModule = finalModule.DeepClone(new CloneContext(cloneContext));
                //PerformanceLogger.Pause(PerformanceStage.DeepClone);

                foreach (var composition in shaderMixinSource.Compositions)
                {
                    //look for the key
                    var foundVars = finalModule.FindAllVariablesByName(composition.Key).Where(value => value.Variable.Qualifiers.Contains(StrideStorageQualifier.Compose)).ToList();

                    if (foundVars.Count > 1)
                    {
                        log.Error(StrideMessageCode.ErrorAmbiguousComposition, new SourceSpan(), composition.Key);
                    }
                    else if (foundVars.Count > 0)
                    {
                        Variable foundVar = foundVars[0].Variable;
                        var moduleMixins = BuildCompositionsDictionary(composition.Value, dictionary, compilationContext, cloneContext, log);
                        if (moduleMixins == null)
                            return null;

                        dictionary.Add(foundVar, moduleMixins);
                    }
                    else
                    {
                        // No matching variable was found
                        // TODO: log a message?
                    }
                }
                return new List<ModuleMixin> { finalModule };
            }


            if (shaderSource is ShaderClassCode)
            {
                var finalModule = compilationContext.GetModuleMixinFromShaderSource(shaderSource);

                //PerformanceLogger.Start(PerformanceStage.DeepClone);
                finalModule = finalModule.DeepClone(new CloneContext(cloneContext));
                //PerformanceLogger.Pause(PerformanceStage.DeepClone);

                return new List<ModuleMixin> { finalModule };
            }

            if (shaderSource is ShaderArraySource)
            {
                var shaderArraySource = shaderSource as ShaderArraySource;
                var compositionArray = new List<ModuleMixin>();
                foreach (var shader in shaderArraySource.Values)
                {
                    var mixin = BuildCompositionsDictionary(shader, dictionary, compilationContext, cloneContext, log);
                    if (mixin == null)
                        return null;
                    compositionArray.AddRange(mixin);
                }
                return compositionArray;
            }

            return null;
        }

        /// <summary>
        /// Link all the stage compositions in case it is referenced at several places.
        /// </summary>
        /// <param name="variable">The variable of the composition.</param>
        /// <param name="composition">The composition.</param>
        /// <param name="dictionary">The already registered compositions.</param>
        /// <param name="extraDictionary">The new compositions.</param>
        /// <param name="log">The logger.</param>
        private static void FullLinkStageCompositions(Variable variable, List<ModuleMixin> composition, CompositionDictionary dictionary, Dictionary<Variable, List<ModuleMixin>> extraDictionary, LoggerResult log)
        {
            var mixin = variable.GetTag(StrideTags.ShaderScope) as ModuleMixin;
            if (mixin != null)
            {
                var className = mixin.MixinName;
                foreach (var item in dictionary)
                {
                    if (item.Key == variable)
                        continue;

                    foreach (var module in item.Value)
                    {
                        if (module.MixinName == className || module.InheritanceList.Any(x => x.MixinName == className))
                        {
                            // add reference
                            var foundVars = module.FindAllVariablesByName(variable.Name).Where(value => value.Variable.Qualifiers.Contains(StrideStorageQualifier.Compose)).ToList();
                            if (foundVars.Count > 1)
                            {
                                log.Error(StrideMessageCode.ErrorAmbiguousComposition, new SourceSpan(), variable.Name);
                            }
                            else if (foundVars.Count > 0)
                            {
                                // if there is already a filled composition, it means that the ShaderMixinSource filled the composition information at two different places
                                // TODO: verify that
                                var foundVar = foundVars[0].Variable;
                                List<ModuleMixin> previousList;
                                if (dictionary.TryGetValue(foundVar, out previousList))
                                {
                                    previousList.AddRange(composition);
                                }
                                else
                                    extraDictionary.Add(foundVars[0].Variable, composition);
                            }
                            else
                            {
                                // No matching variable was found
                                // TODO: log a message?
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get a compilation context based on the macros
        /// </summary>
        /// <param name="mixinToAnalyze">List of mixin to analyze</param>
        /// <param name="log">The log.</param>
        /// <returns>the correct compilation context</returns>
        private ShaderCompilationContext GetCompilationContext(IEnumerable<ModuleMixinInfo> mixinToAnalyze, LoggerResult log)
        {
            var mixinInfos = new HashSet<ModuleMixinInfo>();
            foreach (var mixin in mixinToAnalyze)
                mixinInfos.UnionWith(mixin.MinimalContext);

            var context = new ShaderCompilationContext(log);
            context.Preprocess(mixinInfos);
            return context;
        }

        /// <summary>
        /// Build a dictionary of mixins
        /// </summary>
        /// <param name="finalMixins">a list of mixins</param>
        /// <returns>a dictionary of all the necessary mixins</returns>
        private Dictionary<string, ModuleMixin> BuildMixinDictionary(IEnumerable<ModuleMixin> finalMixins)
        {
            var allMixins = new HashSet<ModuleMixin>();
            foreach (var mixin in finalMixins)
            {
                if (allMixins.All(x => x.MixinName != mixin.MixinName))
                    allMixins.Add(mixin);
            }

            return allMixins.ToDictionary(x => x.MixinName, x => x);
        }

        #endregion
    }
}
