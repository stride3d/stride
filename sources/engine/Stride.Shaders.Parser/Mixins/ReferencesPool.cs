// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Stride.Core;
using Stride.Shaders.Parser.Analysis;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Ast.Stride;

namespace Stride.Shaders.Parser.Mixins
{
    [DebuggerDisplay("Variables[{VariablesReferences.Count}] Methods[{MethodsReferences.Count}]")]
    [DataContract]
    internal class ReferencesPool
    {
        private Dictionary<Variable, HashSet<ExpressionNodeCouple>> variablesReferences = new();
        private Dictionary<MethodDeclaration, HashSet<MethodInvocationExpression>> methodsReferences = new();

        /// <summary>
        /// List of all the variable references
        /// </summary>
        [DataMember]
        public Dictionary<Variable, HashSet<ExpressionNodeCouple>> VariablesReferences => variablesReferences;

        /// <summary>
        /// List of all the variable references
        /// </summary>
        [DataMember]
        public Dictionary<MethodDeclaration, HashSet<MethodInvocationExpression>> MethodsReferences => methodsReferences;

        /// <summary>
        /// Merge the argument references into this one
        /// </summary>
        /// <param name="pool">the ReferencePool</param>
        public void Merge(ReferencesPool pool)
        {
            // merge the VariablesReferences
            foreach (var variableReference in pool.VariablesReferences)
            {
                if (!VariablesReferences.ContainsKey(variableReference.Key))
                    VariablesReferences.Add(variableReference.Key, new HashSet<ExpressionNodeCouple>());

                VariablesReferences[variableReference.Key].UnionWith(variableReference.Value);
            }
            // merge the MethodsReferences
            foreach (var methodReference in pool.MethodsReferences)
            {
                if (!MethodsReferences.ContainsKey(methodReference.Key))
                    MethodsReferences.Add(methodReference.Key, new HashSet<MethodInvocationExpression>());

                MethodsReferences[methodReference.Key].UnionWith(methodReference.Value);
            }
        }

        /// <summary>
        /// Regen the keys because they could have been modified
        /// </summary>
        public void RegenKeys()
        {
            variablesReferences = VariablesReferences.ToDictionary(variable => variable.Key, variable => variable.Value);
            methodsReferences = MethodsReferences.ToDictionary(method => method.Key, variable => variable.Value);
        }

        /// <summary>
        /// Insert a variable reference
        /// </summary>
        /// <param name="variable">the variable</param>
        /// <param name="expression">the reference</param>
        public void InsertVariable(Variable variable, ExpressionNodeCouple expression)
        {
            if (!VariablesReferences.ContainsKey(variable))
                VariablesReferences.Add(variable, new HashSet<ExpressionNodeCouple>());

            // Also add all the variables in that buffer so that they are not removed
            var cbuffer = (ConstantBuffer)variable.GetTag(StrideTags.ConstantBuffer);
            if (cbuffer != null)
            {
                foreach (var otherVariable in cbuffer.Members.OfType<Variable>())
                {
                    if (!VariablesReferences.ContainsKey(otherVariable))
                        VariablesReferences.Add(otherVariable, new HashSet<ExpressionNodeCouple>());
                }
            }

            VariablesReferences[variable].Add(expression);
        }

        /// <summary>
        /// Insert a method reference
        /// </summary>
        /// <param name="methodDeclaration">the method</param>
        /// <param name="expression">the reference</param>
        public void InsertMethod(MethodDeclaration methodDeclaration, MethodInvocationExpression expression)
        {
            if (!MethodsReferences.ContainsKey(methodDeclaration))
                MethodsReferences.Add(methodDeclaration, new HashSet<MethodInvocationExpression>());
            MethodsReferences[methodDeclaration].Add(expression);
        }
    }
}
