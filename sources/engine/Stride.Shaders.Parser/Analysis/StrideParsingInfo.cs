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
        public HashSet<Variable> StageInitializedVariables { get; } = new();

        /// <summary>
        /// All typedefs
        /// </summary>
        public List<Typedef> Typedefs { get; } = new();

        /// <summary>
        /// All structure definitions
        /// </summary>
        public List<StructType> StructureDefinitions { get; } = new();

        /// <summary>
        /// All the base method calls (base.xxx)
        /// </summary>
        public HashSet<MethodInvocationExpression> BaseMethodCalls { get; } = new();

        /// <summary>
        /// All the method calls that are not base
        /// </summary>
        public HashSet<MethodInvocationExpression> ThisMethodCalls { get; } = new();

        /// <summary>
        /// All the method calls to stage methods
        /// </summary>
        public HashSet<MethodInvocationExpression> StageMethodCalls { get; } = new();

        /// <summary>
        /// All foreach statements
        /// </summary>
        public HashSet<StatementNodeCouple> ForEachStatements { get; } = new();

        /// <summary>
        /// References to members of the current shader
        /// </summary>
        public ReferencesPool ClassReferences { get; } = new();

        /// <summary>
        /// Static references to class members
        /// </summary>
        public ReferencesPool StaticReferences { get; } = new();

        /// <summary>
        /// References to extern members
        /// </summary>
        public ReferencesPool ExternReferences { get; } = new();

        /// <summary>
        /// References to stage initialized variables and methods
        /// </summary>
        public ReferencesPool StageInitReferences { get; } = new();

        /// <summary>
        /// Gets navigable nodes (local variables, base class...etc.)
        /// </summary>
        /// <value>The navigable nodes.</value>
        public List<Node> NavigableNodes { get; } = new();

        /// <summary>
        /// List of the static classes
        /// </summary>
        public HashSet<ModuleMixin> StaticClasses { get; } = new();

        #endregion

        #region Public members

        /// <summary>
        /// Error logger
        /// </summary>
        public ParsingResult ErrorsWarnings = null;

        #endregion
    }
}
