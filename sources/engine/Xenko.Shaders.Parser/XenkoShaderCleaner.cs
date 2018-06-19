// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Shaders.Ast.Xenko;
using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Ast.Hlsl;
using Xenko.Core.Shaders.Visitor;

namespace Xenko.Shaders.Parser
{
    internal class XenkoShaderCleaner : ShaderRewriter
    {
        public XenkoShaderCleaner() : base(false, false)
        {
        }

        /// <summary>
        /// Runs this instance on the specified node.
        /// </summary>
        /// <param name="shader">The shader.</param>
        public void Run(Shader shader)
        {
            Visit(shader);
        }

        public void Run(ShaderClassType shaderClassType)
        {
            var shader = new Shader();
            shader.Declarations.Add(shaderClassType);
            Run(shader);
        }

        public override Node DefaultVisit(Node node)
        {
            var qualifierNode = node as IQualifiers;
            if (qualifierNode != null)
            {
                qualifierNode.Qualifiers.Values.Remove(XenkoStorageQualifier.Stream);
                qualifierNode.Qualifiers.Values.Remove(XenkoStorageQualifier.Stage);
                qualifierNode.Qualifiers.Values.Remove(XenkoStorageQualifier.PatchStream);
                qualifierNode.Qualifiers.Values.Remove(XenkoStorageQualifier.Override);
                qualifierNode.Qualifiers.Values.Remove(XenkoStorageQualifier.Clone);
                qualifierNode.Qualifiers.Values.Remove(XenkoStorageQualifier.Stage);
            }

            return base.DefaultVisit(node);
        }
        
        public override Node Visit(AttributeDeclaration attribute)
        {
            if (XenkoAttributes.AvailableAttributes.Contains(attribute.Name))
                return null;

            return attribute;
        }
    }
}
