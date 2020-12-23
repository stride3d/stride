// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Stride.Core.Extensions;
using Stride.Core.Storage;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Shaders.Parser.Utility;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Utility;

namespace Stride.Shaders.Parser.Mixins
{
    internal class StrideShaderLibrary
    {
        #region Delegate

        public delegate ShaderClassType LoadClassSourceDelegate(ShaderClassSource shaderClassSource, Stride.Core.Shaders.Parser.ShaderMacro[] shaderMacros, out ObjectId hash, out ObjectId hashPreprocessSource);

        #endregion
        
        #region Public members

        /// <summary>
        /// List of all the mixin infos
        /// </summary>
        public HashSet<ModuleMixinInfo> MixinInfos = new HashSet<ModuleMixinInfo>();

        /// <summary>
        /// Load function
        /// </summary>
        public ShaderLoader ShaderLoader { get; private set; }

        /// <summary>
        /// Log of all the warnings and errors
        /// </summary>
        public LoggerResult ErrorWarningLog = new LoggerResult();

        /// <summary>
        /// The source hashes
        /// </summary>
        public HashSourceCollection SourceHashes = new HashSourceCollection();

        #endregion

        #region Private members

        private int lastMixIndex = 0;

        /// <summary>
        /// List of contexts per macros
        /// </summary>
        private readonly Dictionary<string, List<ModuleMixinInfo>> mapMacrosToMixins = new Dictionary<string, List<ModuleMixinInfo>>();

        #endregion

        #region Constructor

        public StrideShaderLibrary(ShaderLoader loader)
        {
            ShaderLoader = loader;
        }

        #endregion

        #region Public methods


        public bool AllowNonInstantiatedGenerics { get; set; }

        /// <summary>
        /// Explore the ShaderSource and add the necessary shaders
        /// </summary>
        /// <param name="shaderSource">the ShaderSource to explore</param>
        /// <param name="macros">the macros used</param>
        /// <returns></returns>
        public HashSet<ModuleMixinInfo> LoadShaderSource(ShaderSource shaderSource, Stride.Core.Shaders.Parser.ShaderMacro[] macros)
        {
            var mixinsToAnalyze = new HashSet<ModuleMixinInfo>();
            ExtendLibrary(shaderSource, macros, mixinsToAnalyze);
            ReplaceMixins(mixinsToAnalyze); // no longer replace mixin, redo analysis everytime since there is no way to correctly detect something changed
            return mixinsToAnalyze;
        }

        /// <summary>
        /// Deletes the shader cache for the specified shaders.
        /// </summary>
        /// <param name="modifiedShaders">The modified shaders.</param>
        public void DeleteObsoleteCache(HashSet<string> modifiedShaders)
        {
            var mixinsToDelete = new HashSet<ModuleMixinInfo>();

            foreach (var shaderName in modifiedShaders)
            {
                // find the mixin that depends on this shader
                foreach (var mixin in MixinInfos)
                    if (mixin.ReferencedShaders.Contains(shaderName))
                        mixinsToDelete.Add(mixin);

                // remove the source hash
                SourceHashes.Remove(shaderName);
            }

            // delete the mixins
            foreach (var mixin in mixinsToDelete)
            {
                MixinInfos.Remove(mixin);

                // delete the mixin from the map
                foreach (var macroMap in mapMacrosToMixins)
                    macroMap.Value.Remove(mixin);
            }

            mixinsToDelete.Clear();

            ShaderLoader.DeleteObsoleteCache(modifiedShaders);
        }
        
        #endregion

        #region Private methods

        /// <summary>
        /// Explore the ShaderSource and add the necessary shaders
        /// </summary>
        /// <param name="shaderSource">the ShaderSource to explore</param>
        /// <param name="macros">the macros used</param>
        private void ExtendLibrary(ShaderSource shaderSource, Stride.Core.Shaders.Parser.ShaderMacro[] macros, HashSet<ModuleMixinInfo> mixinToAnalyze)
        {
            if (shaderSource is ShaderMixinSource)
            {
                var newMacros = MergeMacroSets((ShaderMixinSource)shaderSource, macros);
                mixinToAnalyze.Add(GetModuleMixinInfo(shaderSource, newMacros));
                foreach (var composition in ((ShaderMixinSource)shaderSource).Compositions)
                    ExtendLibrary(composition.Value, newMacros, mixinToAnalyze);
            }
            else if (shaderSource is ShaderClassCode)
                mixinToAnalyze.Add(GetModuleMixinInfo(shaderSource, macros));
            else if (shaderSource is ShaderArraySource)
            {
                foreach (var shader in ((ShaderArraySource)shaderSource).Values)
                    ExtendLibrary(shader, macros, mixinToAnalyze);
            }
        }

        /// <summary>
        /// Get the ModuleMixinInfo based on the ShaderSource and the macros. Creates the needed shader if necessary
        /// </summary>
        /// <param name="shaderSource">the ShaderSource</param>
        /// <param name="macros">the macros</param>
        /// <param name="macrosString">the name of the macros</param>
        /// <returns>ModuleMixinInfo.</returns>
        private ModuleMixinInfo GetModuleMixinInfo(ShaderSource shaderSource, Stride.Core.Shaders.Parser.ShaderMacro[] macros, string macrosString = null)
        {
            if (macros == null)
                macros = new Stride.Core.Shaders.Parser.ShaderMacro[0];

            if (macrosString == null)
            {
                macrosString =  string.Join(",", macros.OrderBy(x => x.Name));
            }

            List<ModuleMixinInfo> context;
            if (!mapMacrosToMixins.TryGetValue(macrosString, out context))
            {
                context = new List<ModuleMixinInfo>();
                mapMacrosToMixins.Add(macrosString, context);
            }

            var mixinInfo = context.FirstOrDefault(x => x.AreEqual(shaderSource, macros));
            if (mixinInfo == null)
            {
                mixinInfo = BuildMixinInfo(shaderSource, macros);

                if (mixinInfo.Instanciated)
                {
                    MixinInfos.Add(mixinInfo);
                    context.Add(mixinInfo);

                    mixinInfo.MinimalContext.Add(mixinInfo);

                    if (!mixinInfo.Log.HasErrors)
                    {
                        LoadNecessaryShaders(mixinInfo, macros, macrosString);
                    }
                }
            }

            return mixinInfo;
        }

        /// <summary>
        /// Replace the mixins
        /// </summary>
        /// <param name="mixinInfos">the mixins to verify</param>
        private void ReplaceMixins(HashSet<ModuleMixinInfo> mixinInfos)
        {
            foreach (var mixinInfo in mixinInfos)
                CheckMixinForReplacement(mixinInfo);
        }

        /// <summary>
        /// Check if a previously analyzed instance of the shader can be used
        /// </summary>
        /// <param name="mixinInfo">the ModuleMixinInfo</param>
        private void CheckMixinForReplacement(ModuleMixinInfo mixinInfo)
        {
            // TODO: infinite loop when cross reference (composition & =stage for example)
            // TODO: change ReplacementChecked to enum None/InProgress/Done
            if (mixinInfo.ReplacementChecked)
                return;

            // Check parents and dependencies
            mixinInfo.MinimalContext.Where(x => x != mixinInfo).ForEach(CheckMixinForReplacement);
            
            foreach (var replaceCandidateMixinInfo in MixinInfos.Where(x => x != mixinInfo && x.ShaderSource.Equals(mixinInfo.ShaderSource) && x.HashPreprocessSource == mixinInfo.HashPreprocessSource))
            {
                if (replaceCandidateMixinInfo.Mixin.DependenciesStatus != AnalysisStatus.None)
                {
                    if (replaceCandidateMixinInfo.Mixin.MinimalContext != null)
                    {
                        var noNeedToReplaced = replaceCandidateMixinInfo.Mixin.MinimalContext
                            .Where(dep => dep != replaceCandidateMixinInfo.Mixin)
                            .All(dep => mixinInfo.MinimalContext.FirstOrDefault(x => x.Mixin == dep) != null);
                        if (noNeedToReplaced)
                        {
                            mixinInfo.Mixin = replaceCandidateMixinInfo.Mixin;
                            mixinInfo.MixinAst = replaceCandidateMixinInfo.MixinAst;
                            mixinInfo.MixinGenericName = replaceCandidateMixinInfo.MixinGenericName;
                            break;
                        }
                    }
                }
            }

            mixinInfo.ReplacementChecked = true;
        }

        /// <summary>
        /// Build the ModuleMixinInfo class
        /// </summary>
        /// <param name="shaderSource">the ShaderSource to load</param>
        /// <param name="macros">the macros applied on the source</param>
        /// <returns>the ModuleMixinInfo</returns>
        private ModuleMixinInfo BuildMixinInfo(ShaderSource shaderSource, Stride.Core.Shaders.Parser.ShaderMacro[] macros)
        {
            ModuleMixinInfo mixinInfo = null;
            
            if (shaderSource is ShaderClassCode)
            {
                var shaderClassSource = shaderSource as ShaderClassCode;
                mixinInfo = new ModuleMixinInfo 
                { 
                    ShaderSource = shaderClassSource, 
                    Macros = macros,
                    ReferencedShaders = { shaderClassSource.ClassName }
                };
                LoadMixinFromClassSource(mixinInfo);
            }
            else if (shaderSource is ShaderMixinSource)
            {
                var shaderMixinSource = shaderSource as ShaderMixinSource;

                var shaderName = "Mix" + lastMixIndex;
                ++lastMixIndex;
                var fakeAst = new ShaderClassType(shaderName);
                foreach (var classSource in shaderMixinSource.Mixins)
                {
                    Identifier name;
                    if (classSource.GenericArguments != null && classSource.GenericArguments.Length > 0)
                        name = new IdentifierGeneric(classSource.ClassName, classSource.GenericArguments.Select(x => new Identifier(x.ToString())).ToArray());
                    else
                        name = new Identifier(classSource.ClassName);

                    fakeAst.BaseClasses.Add(new TypeName(name));
                }

                mixinInfo = new ModuleMixinInfo
                    {
                        MixinGenericName = shaderName,
                        Macros = macros, 
                        MixinAst = fakeAst, 
                        ShaderSource =  shaderSource,
                        SourceHash = ObjectId.FromBytes(Encoding.UTF8.GetBytes(shaderName)), 
                        Instanciated = true
                    };
            }

            return mixinInfo;
        }

        /// <summary>
        /// Loads the mixin based on its ShaderSource
        /// </summary>
        /// <param name="mixinInfo">the ModuleMixinInfo</param>
        private void LoadMixinFromClassSource(ModuleMixinInfo mixinInfo)
        {
            var classSource = (ShaderClassCode)mixinInfo.ShaderSource;

            // If we allow to parse non instantiated generics, put empty generic arguments to let the ShaderLoader correctly expand the class
            var shaderClass = ShaderLoader.LoadClassSource(classSource, mixinInfo.Macros, mixinInfo.Log, AllowNonInstantiatedGenerics);

            // If result is null, there was some errors while parsing.
            if (shaderClass == null)
                return;

            var shaderType = shaderClass.Type.DeepClone();

            if (shaderType.ShaderGenerics.Count > 0)
                mixinInfo.Instanciated = false;

            mixinInfo.HashPreprocessSource = shaderClass.PreprocessedSourceHash;
            mixinInfo.SourceHash = shaderClass.SourceHash;

            if (!SourceHashes.ContainsKey(classSource.ClassName))
                SourceHashes.Add(classSource.ClassName, shaderClass.SourceHash);

            // check if it was a generic class and find out if the instanciation was correct
            if (shaderType.GenericParameters.Count > 0)
            {
                if (classSource.GenericArguments == null || classSource.GenericArguments.Length == 0 || shaderType.GenericParameters.Count > classSource.GenericArguments.Length)
                {
                    mixinInfo.Instanciated = false;
                    mixinInfo.Log.Error(StrideMessageCode.ErrorClassSourceNotInstantiated, shaderType.Span, classSource.ClassName);
                }
                else
                {
                    ModuleMixinInfo.CleanIdentifiers(shaderType.GenericParameters.Select(x => x.Name).ToList());
                }
            }

            mixinInfo.MixinAst = shaderType;
            mixinInfo.MixinGenericName = classSource.ClassName;
        }

        /// <summary>
        /// Loads generic classes that may appear in the mixin
        /// </summary>
        /// <param name="mixinInfo">The mixin to investigate</param>
        /// <param name="macros">The macros.</param>
        /// <param name="macrosString">The macros string.</param>
        private void LoadNecessaryShaders(ModuleMixinInfo mixinInfo, Stride.Core.Shaders.Parser.ShaderMacro[] macros, string macrosString)
        {
            if (!mixinInfo.Instanciated)
                return;

            // Look for all the generic calls
            var shaderDependencyVisitor = new ShaderDependencyVisitor(mixinInfo.Log, ShaderLoader.SourceManager);
            shaderDependencyVisitor.Run(mixinInfo.MixinAst);

            foreach (var foundClass in shaderDependencyVisitor.FoundClasses)
            {
                var classSource = new ShaderClassSource(foundClass, null);
                var foundMixinInfo = GetModuleMixinInfo(classSource, macros, macrosString);
                mixinInfo.MinimalContext.UnionWith(foundMixinInfo.MinimalContext);
                mixinInfo.ReferencedShaders.UnionWith(foundMixinInfo.ReferencedShaders);
            }

            foreach (var id in shaderDependencyVisitor.FoundIdentifiers)
            {
                var genericClass = id.Item1;
                ModuleMixinInfo.CleanIdentifiers(genericClass.Identifiers);
                var genericParams = BuildShaderGenericParameters(genericClass);
                var classSource = new ShaderClassSource(genericClass.Text, genericParams);

                var instanciatedClassInfo = GetModuleMixinInfo(classSource, macros, macrosString);
                mixinInfo.MinimalContext.UnionWith(instanciatedClassInfo.MinimalContext);
                mixinInfo.ReferencedShaders.UnionWith(instanciatedClassInfo.ReferencedShaders);

                var newId = new Identifier(instanciatedClassInfo.MixinName);
                if (id.Item2 is TypeName) // in the baseclass list or in a variable declaration
                    (id.Item2 as TypeName).Name = newId;
                else if (id.Item2 is VariableReferenceExpression)
                    (id.Item2 as VariableReferenceExpression).Name = newId;
                else if (id.Item2 is MemberReferenceExpression)
                    (id.Item2 as MemberReferenceExpression).Member = newId;
            }
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Build the array of generic parameters
        /// </summary>
        /// <param name="genericClass">the shader with its generics</param>
        /// <returns>the array of generic parameters</returns>
        private static string[] BuildShaderGenericParameters(IdentifierGeneric genericClass)
        {
            var genericParameters = new List<string>();

            for (int i = 0; i < genericClass.Identifiers.Count; ++i)
            {
                var genericName = GetIdentifierName(genericClass.Identifiers[i]);
                genericParameters.Add(genericName);
            }

            return genericParameters.ToArray();
        }

        /// <summary>
        /// Helper function to get the complete name of an identifier
        /// </summary>
        /// <param name="identifier">the identifier</param>
        /// <returns>the identifier name</returns>
        private static string GetIdentifierName(Identifier identifier)
        {
            string genericName;
            if (identifier is LiteralIdentifier)
                genericName = (identifier as LiteralIdentifier).Value.Value.ToString();
            else if (identifier is IdentifierDot)
            {
                var idDot = identifier as IdentifierDot;
                genericName = idDot.Identifiers.Aggregate("", (current, id) => current + (GetIdentifierName(id) + idDot.Separator));
                genericName = genericName.Substring(0, genericName.Length - idDot.Separator.Length);
            }
            else
                genericName = identifier.Text;

            if (genericName == null)
                throw new Exception(string.Format("Unable to find the name of the generic [{0}]", identifier));

            return genericName;
        }

        /// <summary>
        /// Merge the set of macros in the mixin. The top level macros are always overidden by the child's ones (the one defined in the current ShaderMixinSource).
        /// Also update the macros of the mixin.
        /// </summary>
        /// <param name="mixin">The mixin that will be looked at with the macros.</param>
        /// <param name="macros">The external macros.</param>
        /// <returns>An array with all the macros</returns>
        private Stride.Core.Shaders.Parser.ShaderMacro[] MergeMacroSets(ShaderMixinSource mixin, Stride.Core.Shaders.Parser.ShaderMacro[] macros)
        {
            var newMacros = new List<Stride.Core.Shaders.Parser.ShaderMacro>();

            // get the parent macros
            foreach (var macro in macros)
            {
                newMacros.RemoveAll(x => x.Name == macro.Name);
                newMacros.Add(macro);
            }

            // override with child macros, the mixin's ones
            foreach (var macro in mixin.Macros)
            {
                newMacros.RemoveAll(x => x.Name == macro.Name);
                var tempMacro = new Stride.Core.Shaders.Parser.ShaderMacro(macro.Name, macro.Definition);
                newMacros.Add(tempMacro);
            }

            mixin.Macros = newMacros.Select(x => new ShaderMacro(x.Name, x.Definition)).ToList();
            return newMacros.ToArray();
        }
        
        #endregion
    }
}
