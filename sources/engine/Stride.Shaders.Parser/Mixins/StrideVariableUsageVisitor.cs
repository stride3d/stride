// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using Stride.Core.Shaders.Ast.Stride;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Visitor;

namespace Stride.Shaders.Parser.Mixins
{
    internal class StrideVariableUsageVisitor : ShaderWalker
    {
        private Dictionary<Variable, bool> VariablesUsages;

        public StrideVariableUsageVisitor(Dictionary<Variable, bool> variablesUsages)
            : base(false, false)
        {
            if (variablesUsages == null)
                VariablesUsages = new Dictionary<Variable, bool>();
            else
                VariablesUsages = variablesUsages;
        }

        public void Run(ShaderClassType shaderClassType)
        {
            Visit(shaderClassType);
        }

        public override void Visit(VariableReferenceExpression variableReferenceExpression)
        {
            base.Visit(variableReferenceExpression);
            CheckUsage(variableReferenceExpression.TypeInference.Declaration as Variable);
        }

        public override void Visit(MemberReferenceExpression memberReferenceExpression)
        {
            base.Visit(memberReferenceExpression);
            CheckUsage(memberReferenceExpression.TypeInference.Declaration as Variable);
        }

        private void CheckUsage(Variable variable)
        {
            if (variable == null)
                return;

            if (VariablesUsages.ContainsKey(variable))
                VariablesUsages[variable] = true;
        }
    }
}
