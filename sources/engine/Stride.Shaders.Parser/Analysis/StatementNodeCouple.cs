// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Shaders.Ast;

namespace Xenko.Shaders.Parser.Analysis
{
    [DataContract]
    internal class StatementNodeCouple
    {
        public Statement Statement;
        public Node Node;

        public StatementNodeCouple() : this(null, null) { }

        public StatementNodeCouple(Statement statement, Node node)
        {
            Statement = statement;
            Node = node;
        }
    }
}
