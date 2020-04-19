// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Stride.Core;
using Stride.Shaders.Parser.Analysis;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Core.Shaders.Ast;

namespace Stride.Shaders.Parser.Mixins
{
    [DebuggerDisplay("ModuleMixin {MixinName}")]
    [DataContract]
    internal class ModuleMixin
    {
        #region Public members

        /// <summary>
        /// The name of the mixin
        /// </summary>
        public string MixinName;

        /// <summary>
        /// The name of the mixin
        /// </summary>
        public string MixinGenericName;

        /// <summary>
        /// The shader AST
        /// </summary>
        public ShaderClassType Shader = null;

        /// <summary>
        /// the virtual table before inheritance
        /// </summary>
        public MixinVirtualTable LocalVirtualTable = new MixinVirtualTable();

        /// <summary>
        /// the virtual table after inheritance
        /// </summary>
        public MixinVirtualTable VirtualTable = new MixinVirtualTable();

        /// <summary>
        /// List of all the other declarations
        /// </summary>
        public List<Node> RemainingNodes = new List<Node>();

        /// <summary>
        /// List of all the base mixins
        /// </summary>
        public List<ModuleMixin> BaseMixins = new List<ModuleMixin>();

        /// <summary>
        /// List of all the classes dependencies
        /// </summary>
        public List<ModuleMixin> InheritanceList = new List<ModuleMixin>();

        /// <summary>
        /// List of all the needed mixins to perform semantic analysis
        /// </summary>
        public HashSet<ModuleMixin> MinimalContext = null;

        /// <summary>
        /// List of all the variables dependencies
        /// </summary>
        public Dictionary<Variable, ModuleMixin> VariableDependencies = new Dictionary<Variable, ModuleMixin>();

        /// <summary>
        /// List of all the variable that are initialized at "stage"
        /// </summary>
        public Dictionary<Variable, ModuleMixin> StageInitVariableDependencies = new Dictionary<Variable, ModuleMixin>();

        /// <summary>
        /// Current class member references
        /// </summary>
        public ReferencesPool ClassReferences = new ReferencesPool();

        /// <summary>
        /// Static references
        /// </summary>
        public ReferencesPool StaticReferences = new ReferencesPool();

        /// <summary>
        /// Static references
        /// </summary>
        public ReferencesPool ExternReferences = new ReferencesPool();

        /// <summary>
        /// References through stage init variables
        /// </summary>
        public ReferencesPool StageInitReferences = new ReferencesPool();

        /// <summary>
        /// The result of the parsing
        /// </summary>
        public StrideParsingInfo ParsingInfo { get; set; }

        /// <summary>
        /// Occurrence ID in the inheritance tree
        /// </summary>
        public int OccurrenceId = 0;

        /// <summary>
        /// List of variables that share their name i.e. potential conflicting variables
        /// </summary>
        public HashSet<Variable> PotentialConflictingVariables = new HashSet<Variable>();

        /// <summary>
        /// List of methods that share their signature i.e. potential conflicting methods
        /// </summary>
        public HashSet<MethodDeclaration> PotentialConflictingMethods = new HashSet<MethodDeclaration>();

        /// <summary>
        /// A boolean stating that all the members are stage
        /// </summary>
        public bool StageOnlyClass = false;

        /// <summary>
        /// A flag to state if the mixin dependencies has been analyzed yet
        /// </summary>
        public AnalysisStatus DependenciesStatus = AnalysisStatus.None;

        /// <summary>
        /// A flag to state if the mixin type analysis was performed.
        /// </summary>
        public AnalysisStatus TypeAnalysisStatus = AnalysisStatus.None;

        /// <summary>
        /// A flag to state if the module mixin was built.
        /// </summary>
        public AnalysisStatus ModuleMixinBuildStatus = AnalysisStatus.None;

        /// <summary>
        /// A flag to state if the mixin virtual table was created
        /// </summary>
        public AnalysisStatus VirtualTableStatus = AnalysisStatus.None;

        /// <summary>
        /// A flag to state if the mixin semantic analysis was performed
        /// </summary>
        public AnalysisStatus SemanticAnalysisStatus = AnalysisStatus.None;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="shader">the shader AST</param>
        public void SetShaderAst(ShaderClassType shader)
        {
            if (Shader != null)
                throw new Exception("[ModuleMixin.SetShaderAst] Shader has already been set");
            Shader = shader;
            MixinName = shader.Name.Text;
        }

        #endregion

        #region Public methods
        
        /// <summary>
        /// Finds the method in the mixin
        /// </summary>
        /// <param name="expression">>The expression of the method reference</param>
        /// <returns>a collection of the base methods if found</returns>
        public IEnumerable<MethodDeclarationShaderCouple> FindMethod(MethodInvocationExpression expression)
        {
            foreach (var method in LocalVirtualTable.Methods)
            {
                if (method.Method.IsSameSignature(expression) && method.Method is MethodDefinition)
                    yield return method;
            }
        }

        /// <summary>
        /// Finds the top method - /!\ this is a hack because the mixin may not be analyzed yet /!\
        /// </summary>
        /// <param name="expression">The expression of the method reference</param>
        /// <returns>The base method if found</returns>
        public IEnumerable<MethodDeclarationShaderCouple> FindTopThisFunction(MethodInvocationExpression expression)
        {
            foreach (var method in FindMethod(expression))
                yield return method;

            for (int i = InheritanceList.Count - 1; i >= 0; --i)
            {
                foreach (var method in InheritanceList[i].FindMethod(expression))
                    yield return method;
            }
        }
        
        /// <summary>
        /// Finds the same variable in the ModuleMixin
        /// </summary>
        /// <param name="variableName">the variable name</param>
        /// <returns>the variable declaration if found</returns>
        public IEnumerable<VariableShaderCouple> FindVariableByName(string variableName)
        {
            return LocalVirtualTable.Variables.Where(x => x.Variable.Name.Text == variableName);
        }

        /// <summary>
        /// Find all the variables with this name
        /// </summary>
        /// <param name="variableName">the name of the variable</param>
        /// <returns>A list of all the variables</returns>
        public List<VariableShaderCouple> FindAllVariablesByName(string variableName)
        {
            var resList = FindVariableByName(variableName).ToList();

            for (int i = InheritanceList.Count - 1; i >= 0; --i)
                resList.AddRange(InheritanceList[i].FindVariableByName(variableName));

            return resList;
        }

        /// <summary>
        /// Get the overloaded method from one of its base declaration
        /// </summary>
        /// <param name="methodDeclaration">the MethodDeclaration</param>
        /// <returns>the overloaded MethodDeclaration</returns>
        public MethodDeclaration GetMethodFromDeclaration(MethodDeclaration methodDeclaration)
        {
            var info = (VTableReference)methodDeclaration.GetTag(StrideTags.VirtualTableReference);
            return VirtualTable.GetMethod(info.Shader, info.Slot);
        }

        /// <summary>
        /// Get the overloaded method from its call
        /// </summary>
        /// <param name="expression">the calling Expression</param>
        /// <returns>the overloaded MethodDeclaration</returns>
        public MethodDeclaration GetMethodFromExpression(Expression expression)
        {
            var info = (VTableReference)expression.GetTag(StrideTags.VirtualTableReference);
            return VirtualTable.GetMethod(info.Shader, info.Slot);
        }

        /// <summary>
        /// Get the base MethodDeclaration from its call and the mixin where the call is performed
        /// </summary>
        /// <param name="expression">the calling expression</param>
        /// <param name="mixin">the mixin where the call is performed</param>
        /// <returns>the base MethodDeclaration</returns>
        public MethodDeclaration GetBaseMethodFromExpression(Expression expression, ModuleMixin mixin)
        {
            var info = (VTableReference)expression.GetTag(StrideTags.VirtualTableReference);
            var thisMethod = VirtualTable.GetMethod(info.Shader, info.Slot);

            if (thisMethod == null)
                return null;

            var startIndex = mixin == this ? InheritanceList.Count : InheritanceList.IndexOf(mixin);

            for (int i = startIndex - 1; i >= 0; --i)
            {
                var dep = InheritanceList[i];
                var array = VirtualTable.VirtualTableGroup[dep.MixinName];
                for (int j = 0; j < array.Length; ++j)
                {
                    var method = array[j];
                    if (method == thisMethod)
                        return dep.VirtualTable.VirtualTableGroup[dep.MixinName][j];
                }
            }
            return null;
        }

        #endregion
    }

    /// <summary>
    /// A status needed to analyze the mixin in the correct order within a compilation module
    /// </summary>
    public enum AnalysisStatus
    {
        None,
        InProgress,
        Cyclic,
        Error,
        Complete
    }
}
