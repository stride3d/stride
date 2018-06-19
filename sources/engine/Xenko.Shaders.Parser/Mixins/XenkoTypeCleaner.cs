// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Visitor;

namespace Xenko.Shaders.Parser.Mixins
{
    internal class XenkoTypeCleaner : ShaderWalker
    {
        public XenkoTypeCleaner()
            : base(false, false)
        {
        }

        public void Run(Shader shader)
        {
            Visit(shader);
        }

        public override void DefaultVisit(Node node)
        {
            if (node is Expression || node is TypeBase)
                VisitTypeInferencer((ITypeInferencer)node);

            base.DefaultVisit(node);
        }

        private void VisitTypeInferencer(ITypeInferencer expression)
        {
            expression.TypeInference.Declaration = null;
            expression.TypeInference.TargetType = null;
            expression.TypeInference.ExpectedType = null;
        }
    }
}
