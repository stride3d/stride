// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Shaders.Parser.Analysis;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Shaders.Parser.Utility;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Parser;
using Stride.Core.Shaders.Utility;

namespace Stride.Shaders.Parser.Mixins
{
    internal class ShaderCompilationContext
    {
        #region Private static members

        /// <summary>
        /// A lock to perform a thread safe analysis of the mixins.
        /// </summary>
        private static readonly Object AnalysisLock = new Object();

        #endregion

        #region Public members

        /// <summary>
        /// List of all the mixins
        /// </summary>
        public HashSet<ModuleMixinInfo> MixinInfos = new HashSet<ModuleMixinInfo>();

        /// <summary>
        /// Log of all the warnings and errors
        /// </summary>
        private LoggerResult ErrorWarningLog;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor for cloning
        /// </summary>
        public ShaderCompilationContext(LoggerResult log)
        {
            if (log == null) throw new ArgumentNullException("log");

            ErrorWarningLog = log;
        }

        #endregion

        #region Public methods

        public void Run(){}

        /// <summary>
        /// Runs the first step of the analysis on the context
        /// </summary>
        /// <param name="mixinInfos">the context</param>
        public void Preprocess(HashSet<ModuleMixinInfo> mixinInfos)
        {
            MixinInfos = mixinInfos;

            BuildModuleMixins(MixinInfos);
        }

        /// <summary>
        /// Specifically analyze a module
        /// </summary>
        /// <param name="mixinInfo">the ModuleMixinInfo</param>
        public void Analyze(ModuleMixinInfo mixinInfo)
        {
            ModuleSemanticAnalysisPerMixin(mixinInfo);
        }
        
        /// <summary>
        /// Get the module mixin based on the ShaderSource
        /// </summary>
        /// <param name="shaderSource">the ShaderSource</param>
        /// <returns>the ModuleMixin</returns>
        public ModuleMixin GetModuleMixinFromShaderSource(ShaderSource shaderSource)
        {
            var found = MixinInfos.FirstOrDefault(x => x.ShaderSource.Equals(shaderSource));
            return found == null ? null : found.Mixin;
        }

        #endregion

        #region Private methods
        
        /// <summary>
        /// Get all the declarations of all the mixins
        /// </summary>
        /// <param name="mixinInfos">list of ModuleMixinInfo</param>
        private void BuildModuleMixins(HashSet<ModuleMixinInfo> mixinInfos)
        {
            // type analysis
            foreach (var mixinInfo in mixinInfos)
                PerformTypeAnalysis(mixinInfo);
            foreach (var mixinInfo in mixinInfos)
                BuildModuleMixin(mixinInfo);

            lock (AnalysisLock)
            {
                // reset error status of mixins so we get the same error messages for each compilation
                foreach (var mixinInfo in mixinInfos)
                {
                    var mixin = mixinInfo.Mixin;
                    if (mixin.DependenciesStatus == AnalysisStatus.Error || mixin.DependenciesStatus == AnalysisStatus.Cyclic)
                        mixin.DependenciesStatus = AnalysisStatus.None;
                }

                foreach (var mixinInfo in mixinInfos)
                    BuildMixinDependencies(mixinInfo);

                // build Virtual tables
                foreach (var mixinInfo in mixinInfos)
                    BuildVirtualTables(mixinInfo);
            }
        }

        /// <summary>
        /// Get all the declarations in the mixin
        /// </summary>
        /// <param name="mixinInfo">The mixin info</param>
        private void BuildModuleMixin(ModuleMixinInfo mixinInfo)
        {
            lock (mixinInfo)
            {
                if (mixinInfo.Mixin.ModuleMixinBuildStatus == AnalysisStatus.Complete)
                    return;
                
                if (mixinInfo.Mixin.ModuleMixinBuildStatus != AnalysisStatus.None)
                    throw new Exception("BuildModuleMixin failed for mixin " + mixinInfo.MixinName);

                mixinInfo.Mixin.ModuleMixinBuildStatus = AnalysisStatus.InProgress;

                var mixinAst = mixinInfo.MixinAst;

                var moduleMixin = mixinInfo.Mixin;
                moduleMixin.SetShaderAst(mixinAst);
                moduleMixin.MixinGenericName = mixinInfo.MixinGenericName;

                if (mixinAst != null && mixinInfo.Instanciated)
                {
                    var vtindex = 0;
                    foreach (var member in mixinAst.Members)
                    {
                        if (member is MethodDeclaration)
                        {
                            moduleMixin.LocalVirtualTable.Methods.Add(new MethodDeclarationShaderCouple(member as MethodDeclaration, mixinAst));
                            member.SetTag(StrideTags.VirtualTableReference, new VTableReference { Shader = mixinInfo.MixinName, Slot = vtindex++ });
                        }
                        else if (member is Variable)
                        {
                            var variable = member as Variable;
                            moduleMixin.LocalVirtualTable.Variables.Add(new VariableShaderCouple(variable, mixinAst));
                            // remove null initial values
                            var initValue = variable.InitialValue as VariableReferenceExpression;
                            if (initValue != null && initValue.Name.Text == "null")
                                variable.InitialValue = null;

                        }
                        else if (member is Typedef)
                            moduleMixin.LocalVirtualTable.Typedefs.Add(member as Typedef);
                        else if (member is StructType)
                            moduleMixin.LocalVirtualTable.StructureTypes.Add(member as StructType);
                        else
                            moduleMixin.RemainingNodes.Add(member);

                        // set a tag on the members to easily recognize them from the local declarations/definitions
                        member.SetTag(StrideTags.ShaderScope, moduleMixin);
                        member.SetTag(StrideTags.BaseDeclarationMixin, mixinInfo.MixinName);
                    }

                    // Check name conflicts
                    foreach (var method in moduleMixin.LocalVirtualTable.Methods)
                    {
                        if (moduleMixin.LocalVirtualTable.Methods.Count(x => x.Method.IsSameSignature(method.Method)) > 1) // 1 because the function is in the list
                            ErrorWarningLog.Error(StrideMessageCode.ErrorFunctionRedefined, method.Method.Span, method.Method, mixinInfo.MixinName);

                        if (moduleMixin.LocalVirtualTable.Variables.Any(x => x.Variable.Name.Text == method.Method.Name.Text))
                            ErrorWarningLog.Error(StrideMessageCode.ErrorFunctionVariableNameConflict, method.Method.Span, method.Method, mixinInfo.MixinName);
                    }
                    foreach (var variable in moduleMixin.LocalVirtualTable.Variables)
                    {
                        if (moduleMixin.LocalVirtualTable.Variables.Count(x => x.Variable.Name.Text == variable.Variable.Name.Text) > 1) // 1 because the function is in the list
                            ErrorWarningLog.Error(StrideMessageCode.ErrorFunctionVariableNameConflict, variable.Variable.Span, variable.Variable, mixinInfo.MixinName);

                        if (moduleMixin.LocalVirtualTable.Methods.Any(x => x.Method.Name.Text == variable.Variable.Name.Text))
                            ErrorWarningLog.Error(StrideMessageCode.ErrorVariableNameConflict, variable.Variable.Span, variable.Variable, mixinInfo.MixinName);
                    }
                }

                moduleMixin.MinimalContext = new HashSet<ModuleMixin>(mixinInfo.MinimalContext.Select(x => x.Mixin));

                mixinInfo.Mixin.ModuleMixinBuildStatus = AnalysisStatus.Complete;
            }
        }

        /// <summary>
        /// Get the list of dependencies for the mixin (base classes only)
        /// </summary>
        /// <param name="mixinInfo">the mixin info</param>
        /// <returns>A collection of class names</returns>
        private void BuildMixinDependencies(ModuleMixinInfo mixinInfo)
        {
            var mixin = mixinInfo.Mixin;

            if (mixin.DependenciesStatus == AnalysisStatus.Cyclic || mixin.DependenciesStatus == AnalysisStatus.Error || mixinInfo.Mixin.DependenciesStatus == AnalysisStatus.Complete)
                return;
            if (mixin.DependenciesStatus == AnalysisStatus.InProgress)
            {
                ErrorWarningLog.Error(StrideMessageCode.ErrorCyclicDependency, mixin.Shader.Span, mixin.Shader);
                mixin.DependenciesStatus = AnalysisStatus.Cyclic;
                return;
            }
            if (mixin.DependenciesStatus == AnalysisStatus.None)
            {
                mixin.DependenciesStatus = AnalysisStatus.InProgress;

                foreach (var baseClass in mixin.Shader.BaseClasses)
                {
                    // search based on class name and macros. It is enough since a ShaderMixinSource only have ShaderClassSource as base mixin (and no ShaderMixinSource that may redefine the macros)
                    var bcInfo = MixinInfos.FirstOrDefault(x => x.MixinName == baseClass.Name.Text && AreMacrosEqual(x.Macros, mixinInfo.Macros));

                    if (bcInfo == null)
                    {
                        ErrorWarningLog.Error(StrideMessageCode.ErrorDependencyNotInModule, baseClass.Span, baseClass, mixin.MixinName);
                        mixin.DependenciesStatus = AnalysisStatus.Error;
                        return;
                    }

                    var bc = bcInfo.Mixin;

                    BuildMixinDependencies(bcInfo);
                    if (bc.DependenciesStatus == AnalysisStatus.Error || bc.DependenciesStatus == AnalysisStatus.Cyclic)
                    {
                        mixin.DependenciesStatus = AnalysisStatus.Error;
                        return;
                    }

                    foreach (var dependency in bc.InheritanceList)
                    {
                        if (!mixin.InheritanceList.Contains(dependency))
                            mixin.InheritanceList.Add(dependency);
                    }

                    if (!mixin.InheritanceList.Contains(bc))
                        mixin.InheritanceList.Add(bc);

                    mixin.BaseMixins.Add(bc);

                    if (!bcInfo.Instanciated)
                        mixinInfo.Instanciated = false;
                }

                // do not look for extern keyword but for type name
                foreach (var variable in mixin.LocalVirtualTable.Variables)
                {
                    var variableTypeName = variable.Variable.Type.Name.Text;
                    if (variable.Variable.Type is ArrayType) // support for array of externs
                        variableTypeName = (variable.Variable.Type as ArrayType).Type.Name.Text;
                    
                    var baseClassInfo = MixinInfos.FirstOrDefault(x => x.MixinName == variableTypeName);
                    if (baseClassInfo != null)
                    {
                        variable.Variable.Qualifiers |= Stride.Core.Shaders.Ast.Hlsl.StorageQualifier.Extern; // add the extern keyword but simpler analysis in the future
                        if (variable.Variable.InitialValue is VariableReferenceExpression && (variable.Variable.InitialValue as VariableReferenceExpression).Name.Text == "stage")
                            mixin.StageInitVariableDependencies.Add(variable.Variable, baseClassInfo.Mixin);
                        else
                            mixin.VariableDependencies.Add(variable.Variable, baseClassInfo.Mixin);

                        if (variable.Variable.Type is ArrayType)
                        {
                            var typeArray = variable.Variable.Type as ArrayType;
                            typeArray.Type.TypeInference.Declaration = baseClassInfo.MixinAst;
                        }
                        else
                        {
                            variable.Variable.Type.TypeInference.Declaration = baseClassInfo.MixinAst;
                        }
                    }
                }

                mixin.DependenciesStatus = AnalysisStatus.Complete;
            }
        }

        /// <summary>
        /// Performs type analysis for each mixin
        /// </summary>
        /// <param name="mixinInfo">the ModuleMixinInfo</param>
        private void PerformTypeAnalysis(ModuleMixinInfo mixinInfo)
        {
            lock (mixinInfo)
            {
                if (mixinInfo.Mixin.TypeAnalysisStatus == AnalysisStatus.None)
                {
                    mixinInfo.Mixin.TypeAnalysisStatus = AnalysisStatus.InProgress;

                    // TODO: order + typedef
                    var typeAnalyzer = new StrideTypeAnalysis(new ParsingResult());
                    typeAnalyzer.Run(mixinInfo.MixinAst);
                    mixinInfo.Mixin.TypeAnalysisStatus = AnalysisStatus.Complete;
                }
                else if (mixinInfo.Mixin.TypeAnalysisStatus != AnalysisStatus.Complete)
                {
                    throw new Exception("Type analysis failed for mixin " + mixinInfo.MixinName);
                }
            }
        }

        /// <summary>
        /// Build the virtual table for the specified mixin
        /// </summary>
        /// <param name="mixinInfo">the mixin</param>
        private void BuildVirtualTables(ModuleMixinInfo mixinInfo)
        {
            var mixin = mixinInfo.Mixin;

            if (mixin.VirtualTableStatus == AnalysisStatus.Error || mixin.VirtualTableStatus == AnalysisStatus.Cyclic || mixin.VirtualTableStatus == AnalysisStatus.Complete)
                return;

            if (!mixinInfo.Instanciated)
                return;

            foreach (var dep in mixin.InheritanceList)
            {
                var depInfo = MixinInfos.FirstOrDefault(x => x.Mixin == dep);
                BuildVirtualTables(depInfo);
            }

            // merge the virtual tables
            foreach (var dep in mixin.InheritanceList)
                mixin.VirtualTable.MergeWithLocalVirtualTable(dep.LocalVirtualTable, mixin.MixinName, ErrorWarningLog);

            mixin.VirtualTable.MergeWithLocalVirtualTable(mixin.LocalVirtualTable, mixin.MixinName, ErrorWarningLog);

            foreach (var dep in mixin.InheritanceList)
                mixin.VirtualTable.AddVirtualTable(dep.VirtualTable, dep.MixinName, ErrorWarningLog);
            mixin.VirtualTable.AddFinalDeclarations(mixin.LocalVirtualTable.Methods.Select(x => x.Method).ToList(), mixin.MixinName, ErrorWarningLog);

            foreach (var variable in mixin.VirtualTable.Variables)
            {
                // Compare local against local and inherited against inherited
                var local = variable.Shader == mixin.Shader;

                if (mixin.VirtualTable.Variables.Any(x => (x.Shader == mixin.Shader) == local && x.Variable != variable.Variable && x.Variable.Name.Text == variable.Variable.Name.Text))
                    mixin.PotentialConflictingVariables.Add(variable.Variable);
            }
            foreach (var method in mixin.VirtualTable.Methods)
            {
                // Compare local against local and inherited against inherited
                var local = method.Shader == mixin.Shader;

                if (mixin.VirtualTable.Methods.Any(x => (x.Shader == mixin.Shader) == local && x.Method != method.Method && x.Method.IsSameSignature(method.Method)))
                    mixin.PotentialConflictingMethods.Add(method.Method);
            }

            CheckStageClass(mixin);

            mixin.VirtualTableStatus = AnalysisStatus.Complete;
        }

        /// <summary>
        /// Check if the class is stage
        /// </summary>
        /// <param name="mixin">the ModuleMixin to check</param>
        private void CheckStageClass(ModuleMixin mixin)
        {
            mixin.StageOnlyClass = mixin.VirtualTable.Variables.All(x => x.Variable.Qualifiers.Contains(StrideStorageQualifier.Stage)
                                                                      && !x.Variable.Qualifiers.Contains(StrideStorageQualifier.Compose)) // composition variable can be stage but the classes behind may not be.
                                && mixin.VirtualTable.Methods.All(x => x.Method.Qualifiers.Contains(StrideStorageQualifier.Stage)
                                                                    && !x.Method.Qualifiers.Contains(StrideStorageQualifier.Clone));
        }

        /// <summary>
        /// Performs an semantic analysis of the mixin inside its own context
        /// </summary>
        /// <param name="mixinInfo">The mixin to analyze</param>
        private void ModuleSemanticAnalysisPerMixin(ModuleMixinInfo mixinInfo)
        {
            if (mixinInfo == null)
                return;

            var mixin = mixinInfo.Mixin;

            if (mixin.DependenciesStatus != AnalysisStatus.Complete || mixin.VirtualTableStatus != AnalysisStatus.Complete)
                return;

            if (mixin.SemanticAnalysisStatus == AnalysisStatus.Complete)
                return;
            if (mixin.SemanticAnalysisStatus == AnalysisStatus.InProgress)
            {
                ErrorWarningLog.Error(StrideMessageCode.ErrorCyclicDependency, mixin.Shader.Span, mixin.Shader);
                return;
            }
            if (!mixinInfo.Instanciated)
                return;
            
            mixin.SemanticAnalysisStatus = AnalysisStatus.InProgress;

            // analyze the base mixins
            foreach (var baseClass in mixin.BaseMixins)
            {
                var baseClassInfo = MixinInfos.FirstOrDefault(x => x.Mixin == baseClass);
                ModuleSemanticAnalysisPerMixin(baseClassInfo);

                if (baseClassInfo.Mixin.SemanticAnalysisStatus == AnalysisStatus.Error || baseClassInfo.Mixin.SemanticAnalysisStatus == AnalysisStatus.Cyclic)
                {
                    mixin.SemanticAnalysisStatus = AnalysisStatus.Error;
                    return;
                }

                if (!baseClassInfo.Instanciated)
                {
                    mixinInfo.Instanciated = false;
                    mixin.SemanticAnalysisStatus = AnalysisStatus.None;
                    return;
                }

                if (mixin.LocalVirtualTable.CheckNameConflict(baseClass.VirtualTable, ErrorWarningLog))
                {
                    mixin.SemanticAnalysisStatus = AnalysisStatus.Error;
                    return;
                }
            }

            // analyze the extern mixins
            foreach (var externMixin in mixin.VariableDependencies)
            {
                var externMixinInfo = MixinInfos.FirstOrDefault(x => x.Mixin == externMixin.Value);
                ModuleSemanticAnalysisPerMixin(externMixinInfo);
                if (externMixinInfo.Mixin.SemanticAnalysisStatus == AnalysisStatus.Error || externMixinInfo.Mixin.SemanticAnalysisStatus == AnalysisStatus.Cyclic)
                {
                    mixin.SemanticAnalysisStatus = AnalysisStatus.Error;
                    return;
                }
            }

            var compilationContext = MixinInfos.Select(x => x.Mixin).ToList();

            mixin.ParsingInfo = StrideSemanticAnalysis.RunAnalysis(mixin, compilationContext);

            var staticStageMixins = new List<ModuleMixin>();
            staticStageMixins.AddRange(mixin.ParsingInfo.StaticReferences.VariablesReferences.Select(x => x.Key.GetTag(StrideTags.ShaderScope) as ModuleMixin));
            staticStageMixins.AddRange(mixin.ParsingInfo.StaticReferences.MethodsReferences.Select(x => x.Key.GetTag(StrideTags.ShaderScope) as ModuleMixin));
            staticStageMixins.AddRange(mixin.ParsingInfo.StageInitReferences.VariablesReferences.Select(x => x.Key.GetTag(StrideTags.ShaderScope) as ModuleMixin));
            staticStageMixins.AddRange(mixin.ParsingInfo.StageInitReferences.MethodsReferences.Select(x => x.Key.GetTag(StrideTags.ShaderScope) as ModuleMixin));
            staticStageMixins.RemoveAll(x => x == mixin);

            foreach (var dep in staticStageMixins)
            {
                var depInfo = MixinInfos.FirstOrDefault(x => x.Mixin == dep);
                ModuleSemanticAnalysisPerMixin(depInfo);
                if (dep.SemanticAnalysisStatus == AnalysisStatus.Error || dep.SemanticAnalysisStatus == AnalysisStatus.Cyclic)
                {
                    mixin.SemanticAnalysisStatus = AnalysisStatus.Error;
                    return;
                }
            }

            // check the extern stage references (but do not change the type inference)
            var externList = new List<ModuleMixin>();

            // NOTE: we cannot use the members .Values of .Keys because it internally modifies the dictionary, creating a Dictionary<K,T>.ValueCollection which cannot be cloned (no default constructor).
            // This results in a exception in the DeepClone code.
            mixin.InheritanceList.ForEach(dep => externList.AddRange(dep.VariableDependencies.Select(x => x.Value)));
            externList.AddRange(mixin.VariableDependencies.Select(x => x.Value));
            externList.ForEach(ext => CheckReferencesFromExternMixin(ext, mixin));

            mixin.ParsingInfo.ErrorsWarnings.CopyTo(ErrorWarningLog);

            mixin.SemanticAnalysisStatus = AnalysisStatus.Complete;
        }

        /// <summary>
        /// Check that the stage function calls are possible and that the stage declared variable have a correct type
        /// </summary>
        /// <param name="externMixin">the mixin to look into</param>
        /// <param name="contextMixin">the root mixin</param>
        private void CheckReferencesFromExternMixin(ModuleMixin externMixin, ModuleMixin contextMixin)
        {
            // test that the root mixin has the correct type
            foreach (var variable in externMixin.ParsingInfo.StageInitializedVariables)
            {
                if (variable.Type.Name.Text != contextMixin.MixinName && contextMixin.InheritanceList.All(x => x.MixinName == variable.Type.Name.Text)) // since it is the same AST, compare the object?
                    ErrorWarningLog.Error(StrideMessageCode.ErrorExternStageVariableNotFound, variable.Span, variable, externMixin.MixinName);
            }

            foreach (var stageCall in externMixin.ParsingInfo.StageMethodCalls)
            {
                var decl = contextMixin.FindTopThisFunction(stageCall).FirstOrDefault();
                if (decl == null)
                    ErrorWarningLog.Error(StrideMessageCode.ErrorExternStageFunctionNotFound, stageCall.Span, stageCall, externMixin.MixinName, contextMixin.MixinName);
            }

            // recursive calls
            foreach (var mixin in externMixin.InheritanceList)
            {
                CheckReferencesFromExternMixin(mixin, contextMixin);

                foreach (var externModule in mixin.VariableDependencies)
                    CheckReferencesFromExternMixin(externModule.Value, contextMixin);
            }

            foreach (var externModule in externMixin.VariableDependencies)
                CheckReferencesFromExternMixin(externModule.Value, contextMixin);
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Tests the equality of the macro sets.
        /// </summary>
        /// <param name="macros0">The first set of macros.</param>
        /// <param name="macros1">The second set of macros.</param>
        /// <returns>True if the sets match, false otherwise.</returns>
        private static bool AreMacrosEqual(Stride.Core.Shaders.Parser.ShaderMacro[] macros0, Stride.Core.Shaders.Parser.ShaderMacro[] macros1)
        {
            return macros0.All(macro => macros1.Any(x => x.Name == macro.Name && x.Definition == macro.Definition)) && macros1.All(macro => macros0.Any(x => x.Name == macro.Name && x.Definition == macro.Definition));
        }

        #endregion
    }

    [DataContract]
    internal class VTableReference
    {
        public string Shader = "";

        public int Slot = -1;

        public override bool Equals(object obj)
        {
            var vtr = obj as VTableReference;
            if (vtr == null)
                return false;

            return Slot == vtr.Slot && Shader == vtr.Shader;
        }

        public override int GetHashCode()
        {
            return Slot;
            //return (Shader.GetHashCode() * 397) ^ (Slot + 2);
        }
    }
}
