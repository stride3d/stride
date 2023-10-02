// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;
using Stride.Shaders.Parser.Mixins;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Parser;

namespace Stride.Shaders.Parser.Analysis
{
    [DataContract]
    internal class StrideParsingInfo
    {
        #region Public properties

        /// <summary>
        /// Variables that referenced the stage class ( "= stage" )
        /// </summary>
        [DataMember]
        public HashSet<Variable> StageInitializedVariables { get; private set; }

        /// <summary>
        /// All typedefs
        /// </summary>
        [DataMember]
        public List<Typedef> Typedefs { get; private set; }

        /// <summary>
        /// All structure definitions
        /// </summary>
        [DataMember]
        public List<StructType> StructureDefinitions { get; private set; }

        /// <summary>
        /// All the base method calls (base.xxx)
        /// </summary>
        [DataMember]
        public HashSet<MethodInvocationExpression> BaseMethodCalls { get; private set; }

        /// <summary>
        /// All the method calls that are not base
        /// </summary>
        [DataMember]
        public HashSet<MethodInvocationExpression> ThisMethodCalls { get; private set; }

        /// <summary>
        /// All the method calls to stage methods
        /// </summary>
        [DataMember]
        public HashSet<MethodInvocationExpression> StageMethodCalls { get; private set; }

        /// <summary>
        /// All foreach statements
        /// </summary>
        [DataMember]
        public HashSet<StatementNodeCouple> ForEachStatements { get; private set; }

        /// <summary>
        /// References to members of the current shader
        /// </summary>
        [DataMember]
        public ReferencesPool ClassReferences { get; private set; }

        /// <summary>
        /// Static references to class members
        /// </summary>
        [DataMember]
        public ReferencesPool StaticReferences { get; private set; }

        /// <summary>
        /// References to extern members
        /// </summary>
        [DataMember]
        public ReferencesPool ExternReferences { get; private set; }

        /// <summary>
        /// References to stage initialized variables and methods
        /// </summary>
        [DataMember]
        public ReferencesPool StageInitReferences { get; private set; }

        /// <summary>
        /// Gets navigable nodes (local variables, base class...etc.)
        /// </summary>
        /// <value>The navigable nodes.</value>
        [DataMember]
        public List<Node> NavigableNodes { get; private set; }

        /// <summary>
        /// List of the static classes
        /// </summary>
        [DataMember]
        public HashSet<ModuleMixin> StaticClasses { get; private set; }

        #endregion

        #region Public members

        /// <summary>
        /// Error logger
        /// </summary>
        public ParsingResult ErrorsWarnings = null;

        #endregion

        #region Constructor

        public StrideParsingInfo()
        {
            StageInitializedVariables = new HashSet<Variable>();
            Typedefs = new List<Typedef>();
            StructureDefinitions = new List<StructType>();
            BaseMethodCalls = new HashSet<MethodInvocationExpression>();
            ThisMethodCalls = new HashSet<MethodInvocationExpression>();
            StageMethodCalls = new HashSet<MethodInvocationExpression>();
            ForEachStatements = new HashSet<StatementNodeCouple>();
            ClassReferences = new ReferencesPool();
            StaticReferences = new ReferencesPool();
            ExternReferences = new ReferencesPool();
            StageInitReferences = new ReferencesPool();
            StaticClasses = new HashSet<ModuleMixin>();
            NavigableNodes = new List<Node>();
        }

        #endregion
    }
}
