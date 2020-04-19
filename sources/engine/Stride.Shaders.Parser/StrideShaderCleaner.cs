// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Shaders.Ast.Stride;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Visitor;

namespace Stride.Shaders.Parser
{
    internal class StrideShaderCleaner : ShaderRewriter
    {
        public StrideShaderCleaner() : base(false, false)
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
                qualifierNode.Qualifiers.Values.Remove(StrideStorageQualifier.Stream);
                qualifierNode.Qualifiers.Values.Remove(StrideStorageQualifier.Stage);
                qualifierNode.Qualifiers.Values.Remove(StrideStorageQualifier.PatchStream);
                qualifierNode.Qualifiers.Values.Remove(StrideStorageQualifier.Override);
                qualifierNode.Qualifiers.Values.Remove(StrideStorageQualifier.Clone);
                qualifierNode.Qualifiers.Values.Remove(StrideStorageQualifier.Stage);
            }

            return base.DefaultVisit(node);
        }
        
        public override Node Visit(AttributeDeclaration attribute)
        {
            if (StrideAttributes.AvailableAttributes.Contains(attribute.Name))
                return null;

            return attribute;
        }
    }
}
