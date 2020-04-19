// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Shaders.Ast;

namespace Stride.Shaders.Parser.Analysis
{
    [DataContract]
    internal class ExpressionNodeCouple
    {
        public Expression Expression;
        public Node Node;

        public ExpressionNodeCouple() : this(null, null) {}

        public ExpressionNodeCouple(Expression expression, Node node)
        {
            Expression = expression;
            Node = node;
        }
    }
}
