// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Stride.Core.Shaders.Ast.Stride;
using Stride.Shaders.Parser.Utility;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Utility;
using Stride.Core.Shaders.Visitor;

namespace Stride.Shaders.Parser.Mixins
{
    class ShaderDependencyVisitor : ShaderWalker
    {
        public HashSet<Tuple<IdentifierGeneric, Node>> FoundIdentifiers = new HashSet<Tuple<IdentifierGeneric, Node>>();

        public HashSet<string> FoundClasses = new HashSet<string>();

        private readonly LoggerResult log;

        /// <summary>
        /// Name of the classes
        /// </summary>
        //private HashSet<string> shaderClassNames;
        private readonly ShaderSourceManager sourceManager;

        public ShaderDependencyVisitor(LoggerResult log, ShaderSourceManager sourceManager)
            : base(false, true)
        {
            if (log == null) throw new ArgumentNullException("log");
            if (sourceManager == null) throw new ArgumentNullException("sourceManager");

            this.log = log;
            this.sourceManager = sourceManager;
        }

        public void Run(ShaderClassType shaderClassType)
        {
            Visit(shaderClassType);
        }

        public override void Visit(IdentifierGeneric identifier)
        {
            base.Visit(identifier);

            FoundIdentifiers.Add(Tuple.Create(identifier, ParentNode));
        }

        public override void Visit(VariableReferenceExpression variableReferenceExpression)
        {
            base.Visit(variableReferenceExpression);

            if (sourceManager.IsClassExists(variableReferenceExpression.Name.Text))
                FoundClasses.Add(variableReferenceExpression.Name.Text);
        }

        public override void Visit(MemberReferenceExpression memberReferenceExpression)
        {
            base.Visit(memberReferenceExpression);

            if (sourceManager.IsClassExists(memberReferenceExpression.Member.Text))
                FoundClasses.Add(memberReferenceExpression.Member.Text);
        }

        public override void DefaultVisit(Node node)
        {
            base.DefaultVisit(node);

            var typeBase = node as TypeBase;
            if (typeBase != null)
            {
                if (sourceManager.IsClassExists(typeBase.Name.Text))
                {
                    FoundClasses.Add(typeBase.Name.Text);
                }
                else if (typeBase is ShaderTypeName)
                {
                    // Special case for ShaderTypeName as we must generate an error if it is not found
                    log.Error(StrideMessageCode.ErrorClassNotFound, typeBase.Span, typeBase.Name.Text);
                }
            }
        }
    }
}
