using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Directives
{
    public abstract class DirectiveNode {}
    public class CodeSnippet : DirectiveNode
    {
        public string? Code { get; set; }
    }
    public abstract class DirectiveWithCodeNode : DirectiveNode 
    {
        List<CodeSnippet> CodeSnippets { get; set; } = new();
    }
    public abstract class DirectiveWithChildren : DirectiveWithCodeNode
    {
        List<DirectiveNode> Children { get; set; } = new();
    }

    public class IfDefNode : DirectiveWithChildren
    {
        public string ValueName { get; set; }
    }
    public class IfNDefNode : DirectiveWithChildren
    {
        public string ValueName { get; set; }
    }
    public class SimpleDefineNode : DirectiveNode
    {
        public string Name { get; set; }
    }

    public class DefineNode<T> : DirectiveNode
    {
        public string Name { get; set; }
        public T Value { get; set; }
    }

    public class IfNode : DirectiveWithChildren
    {
        public ExpressionNode Expression { get; set; }
    }
    public class ElIfNode : DirectiveWithChildren
    {
        public ExpressionNode Expression { get; set; }
    }
    public class ElseNode : DirectiveNode
    {
    }
}
