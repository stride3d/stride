// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Shaders.Ast.Xenko;
using Xenko.Core.Shaders.Ast;
using Xenko.Core.Shaders.Visitor;

namespace Xenko.Shaders.Parser.Mixins
{
    internal class XenkoTagCleaner : ShaderWalker
    {
        public XenkoTagCleaner()
            : base(false, false)
        {
        }

        public void Run(ShaderClassType shader)
        {
            Visit(shader);
        }

        public override void DefaultVisit(Node node)
        {
            // Keeping it for ShaderLinker (removed by XenkoShaderCleaner)
            //node.RemoveTag(XenkoTags.ConstantBuffer);
            node.RemoveTag(XenkoTags.ShaderScope);
            node.RemoveTag(XenkoTags.StaticRef);
            node.RemoveTag(XenkoTags.ExternRef);
            node.RemoveTag(XenkoTags.StageInitRef);
            node.RemoveTag(XenkoTags.CurrentShader);
            node.RemoveTag(XenkoTags.VirtualTableReference);
            node.RemoveTag(XenkoTags.BaseDeclarationMixin);
            node.RemoveTag(XenkoTags.ShaderScope);
            base.DefaultVisit(node);
        }
    }
}
