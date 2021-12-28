// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Visitor;

namespace Stride.Core.Shaders.Convertor
{
    public class CallstackVisitor : ShaderRewriter
    {

        public CallstackVisitor() : base(true, true)
        {
        }

        public virtual void Run(MethodDefinition methodEntry)
        {
            base.Visit(methodEntry);
        }

        public override Node Visit(MethodInvocationExpression methodInvocationExpression)
        {
            // Visit childrens first
            base.Visit(methodInvocationExpression);

            var nestedMethodRef = methodInvocationExpression.Target as VariableReferenceExpression;

            if (nestedMethodRef != null)
            {
                var nestedMethod = nestedMethodRef.TypeInference.Declaration as MethodDefinition;
                if (nestedMethod != null && !nestedMethod.IsBuiltin)
                {
                    this.ProcessMethodInvocation(methodInvocationExpression, nestedMethod);
                }
            }

            return methodInvocationExpression;
        }

        protected virtual void ProcessMethodInvocation(MethodInvocationExpression invoke, MethodDefinition method)
        {
            this.VisitDynamic(method);
        }
    }
}
