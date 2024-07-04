using System.Text;

namespace Stride.Shaders.Parsing.SDSL.AST;

public abstract class Node(TextLocation info)
{
    public TextLocation Info { get; set; } = info;
}
public class ValueNode(TextLocation info) : Node(info)
{
    public string? Type { get; set; } = null;
}
public class NoNode() : Node(new());


public class CodeSnippets() : Node(new())
{
    public List<Code> Snippets { get; set; } = [];

    public string ToCode()
    {
        var builder = new StringBuilder();
        foreach (var s in Snippets)
            builder.Append(s.Info.Text);
        return builder.ToString();
    }
}

public class CodeNode(TextLocation info) : Node(info);
public class Comment(TextLocation info) : CodeNode(info);
public class Code(TextLocation info) : CodeNode(info);