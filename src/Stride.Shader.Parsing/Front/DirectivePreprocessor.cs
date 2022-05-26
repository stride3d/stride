using Stride.Shader.Parsing.AST.Directives;
using Stride.Shader.Parsing.Grammars.Comments;
using Stride.Shader.Parsing.Grammars.Directive;
using Stride.Shader.Parsing.Grammars.Macros;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing;

public class DirectivePreprocessor
{
    Dictionary<string, object> macros;
    public CommentGrammar CommentParser { get; set; }
    public DirectiveGrammar DirectivesParser { get; set; } = new();
    public MacroGrammar MacrosParser { get; set; }
    public Dictionary<string,object> Macros 
    {
        get => macros;
        set { macros = value; MacrosParser = new(macros.Keys.ToArray()); }
    }

    public DirectivePreprocessor()
    {
        DirectivesParser = new();
        CommentParser = new();
        MacrosParser = new();
        macros = new();
    }

    public string RemoveComments(string code)
    {
        var comments = CommentParser.Match(code);
        var uncommentedCode = new StringBuilder();
        if (!comments.Matches.Any(x => x.Name == "Comment"))
        {
            return code;
        }
        else
        {
            foreach (var m in comments.Matches)
            {
                if (m.Name == "ActualCode")
                {
                    uncommentedCode.AppendLine(m.StringValue);
                }
            }
            return uncommentedCode.ToString();
        }
    }

    public DirectiveToken ParseDirectives(in string code)
    {
        var parseTree = DirectivesParser.Match(code);
        if (!parseTree.Success)
            throw new Exception(parseTree.ErrorMessage);
        return DirectiveToken.GetToken(parseTree["Directives"]);
    }

    public string PreProcess(string shader)
    {
        var uncommentedCode = RemoveComments(shader);
        var AST = ParseDirectives(uncommentedCode);
        AST.EvaluateMacros(macros);
        var afterDirectives = new StringBuilder();
        DirectiveToken.Evaluate(AST, macros, afterDirectives);
        var matches = MacrosParser.Match(afterDirectives.ToString());
        if (!matches.Success)
            throw new Exception(matches.ErrorMessage);
        var replaceDefines = new StringBuilder();
        foreach(var m in matches.Matches)
            replaceDefines.Append(m.Name == "ActualCode" ? m.StringValue : macros[m.StringValue]);
        return replaceDefines.ToString();
    }

}
