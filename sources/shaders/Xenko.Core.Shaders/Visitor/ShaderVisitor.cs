// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Shaders.Ast;

namespace Xenko.Core.Shaders.Visitor
{
    public abstract partial class ShaderVisitor<TResult> : VisitorBase
    {
        protected ShaderVisitor(bool buildScopeDeclaration, bool useNodeStack) : base(buildScopeDeclaration, useNodeStack)
        {
        }

        public virtual TResult DefaultVisit(Node node)
        {
            return default(TResult);
        }

        public virtual TResult VisitNode(Node node)
        {
            if (node != null)
                return node.Accept(this);

            return default(TResult);
        }
    }

    public abstract partial class ShaderRewriter : ShaderVisitor<Node>
    {
        protected ShaderRewriter(bool buildScopeDeclaration, bool useNodeStack) : base(buildScopeDeclaration, useNodeStack)
        {
        }

        protected sealed override Node DoVisitNode(Node node)
        {
            return VisitNode(node);
        }

        public override Node DefaultVisit(Node node)
        {
            return node;
        }
    }

    public partial class ShaderCloner : ShaderRewriter
    {
        public ShaderCloner() : base(false, false)
        {
        }
    }

    /// <summary>
    /// A Generic Visitor.
    /// </summary>
    /// <remarks>
    /// An derived classs need to set the Iterator with this instance.
    /// </remarks>
    public abstract partial class ShaderVisitor : VisitorBase
    {
        protected ShaderVisitor(bool buildScopeDeclaration, bool useNodeStack) : base(buildScopeDeclaration, useNodeStack)
        {
        }

        protected sealed override Node DoVisitNode(Node node)
        {
            VisitNode(node);
            return node;
        }

        public virtual void VisitNode(Node node)
        {
            node?.Accept(this);
        }

        public virtual void DefaultVisit(Node node)
        {
        }
    }

    public abstract partial class ShaderWalker : ShaderVisitor
    {
        protected ShaderWalker(bool buildScopeDeclaration, bool useNodeStack) : base(buildScopeDeclaration, useNodeStack)
        {
        }
    }
}
