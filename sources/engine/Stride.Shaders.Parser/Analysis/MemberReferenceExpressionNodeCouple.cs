// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Shaders.Ast;

namespace Stride.Shaders.Parser.Analysis
{
    public class MemberReferenceExpressionNodeCouple
    {
        public MemberReferenceExpression Member;
        public Node Node;

        public MemberReferenceExpressionNodeCouple() : this(null, null) { }

        public MemberReferenceExpressionNodeCouple(MemberReferenceExpression member, Node node)
        {
            Member = member;
            Node = node;
        }
    }
}
